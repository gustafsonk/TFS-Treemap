/* <----------------------BEGIN--- GLOBALS ------------------------------> */
// Data references, used for data interpretation, resets on treemap creation.
var json;
var dataLevels;
var levelSplitter = '-';

// Depth references, used for calculations/checks, resets on treemap creation.
var currentDepth;
var dataDepth;

// Used for tracking the breadcrumb trail, resets on treemap creation.
var nodePath;

// Used for efficient highlighting of nodes on mouse hover, resets on a full refresh or when the browser destroys a treemap.
var highlightNodes;

// Convenience reference for checking navigation.
var currentNode;

// Convenience reference for the visualization.
var treemap;

// Used to show the name of the active treemap.
var treemapName = '';

// Convenience reference, used for manipulating the table in the build dialog.
var slickGrid;

// Used for positioning some elements.
var breadcrumbHeight = 26;
var titleHeight = 20;
var aboveFilterTree = 240; // Pseudo-sum of several element heights, a hack for maxed out accordions in dialogs.
var aboveLoadTree = 145; // Same as above.

// Used to prevent multiple requests.
var isTreeRefreshDone = true;
var isSaveDone = true;
var isDeleteDone = true;

// Confirm on overwrite for saves.
var confirmedSave = false;
var lastFailedSaveName = '';

// Default, adjustable settings.
var isFullScreen = true;
var isToolbarHidden = false;
var isVelocityFlag = false;
var levelsToShow = 3;
var labelsToShow = [0, 3];
var layoutType = 'row';
var dataType = 'Sprint' + levelSplitter + 'Team' + levelSplitter + 'Theme' + levelSplitter + 'PBI';

function networkError(message) {
    alert('There was a network error. :(\n' + message);
}
/* <-----------------------END---- GLOBALS ------------------------------> */


/* <----------------------BEGIN--- TREEMAP CREATION ------------------------------> */
// jQuery event handler for starting on page load.
$(document).ready(function () {
    // Crude check for dealing with IE.
    if (isBrowserUnsupported()) {
        return;
    }

    initFilterTree(); // Begin loading the filter.
    initSlickGrid(); // Load the default data build settings.
    initUIElements(); // Rest doesn't matter so much.
});

// Should only run when it is removed from the DOM.
function initTreeMap(json) {
    // Store a global reference.
    this.json = json;

    treemap = new $jit.TM.Squarified({
        injectInto: 'treemap', // Div id for the visualization.
        titleHeight: titleHeight, // Parent box title heights.
        offset: 2, // Thickness of surrounding box offsets.
        levelsToShow: levelsToShow, // Show only x levels of the tree at once.
        labelsToShow: labelsToShow, // Level of labels to show with respect to the highest shown node.
        Events: {
            enable: true,
            onClick: function (node) {
                if (node) {
                    enterNode(node);
                }
            },
            onRightClick: function () {
                exitNode();
            },
            onDragEnd: function (node, junk, mouse) { // Some mice have difficulty clicking without initiating a drag.
                // If the drag ends in the node it started in.
                if (node.id === mouse.target.id) {
                    if (mouse.which === 1) { // Left mouse button.
                        enterNode(node);
                    }
                    else if (mouse.which === 3) { // Right mouse button.
                        exitNode();
                    }
                }
            }
        },
        Tips: { // Tooltips.
            enable: true,
            offsetX: 20,
            offsetY: 20,
            onShow: function (tip, node) {
                // Represents the node's type.
                var prefix = node.id.charAt(0);
                var type = getTypeFromPrefix(prefix);

                // Label the node's type.
                var html = '<div class="tip-type">' + type + '</div>';

                // Add the item's title.
                html += '<div class="tip-title">' + node.name + '</div>';

                // Add a table of information.
                html += '<table>';
                if (prefix === 'b') { // PBI tooltip.
                    html += TooltipRowHtml('ID', '#' + node.data.i);
                }

                if (prefix === 'h' || prefix === 'b') { // Theme/PBI tooltip.
                    html += TooltipRowHtml('Priority', node.data.p);
                }

                if (prefix !== 'b') { // Not-PBI tooltip.
                    html += TooltipRowHtml('Total Effort', node.data.e);
                }
                else { // PBI tooltip.
                    html += TooltipRowHtml('Effort', node.data.e == 0 ? '<b><span style="color:#CC2200">undefined</span></b>' : node.data.e);

                    // Special colors for the PBI state.
                    html += TooltipRowHtml('State', '<b><span style="color:' + getColorFromState(node.data.s) + '">' + node.data.s + '</span></b>');
                }
                html += '</table>';

                tip.innerHTML = html;
            }
        },
        onCreateLabel: function (domElement, node) {
            // Set the name of the label.
            var encodedName = HtmlEncode(node.name);
            if (isVelocityFlag && node.data.v !== undefined && (node.data.e > node.data.v || node.data.e < node.data.v * .5)) {
                encodedName = '<span style="color:red">' + encodedName + '</span>';
            }
            domElement.innerHTML = encodedName;

            // Reference it in a collection for efficient highlighting.
            var nodeCollection = highlightNodes[encodedName];
            if (nodeCollection) {
                nodeCollection.push(domElement);
            }
            else {
                highlightNodes[encodedName] = [domElement];
                nodeCollection = highlightNodes[encodedName];
            }

            // Give the related nodes a border color on hover.
            domElement.onmouseover = function () {
                $.each(nodeCollection, function () {
                    if (domElement === this) {
                        this.style.borderColor = 'cyan';
                    }
                    else {
                        this.style.borderColor = 'yellow';
                    }
                });
            };

            // Remove the border colors on exit.
            domElement.onmouseout = function () {
                $.each(nodeCollection, function () {
                    this.style.borderColor = '';
                });
            };
        },
        onPlaceLabel: function (domElement, node) {
            // Switch to the highest depth available if the expected depth is too large.
            var lowestDepth = levelsToShow + currentDepth;
            if (lowestDepth > dataDepth) {
                lowestDepth = dataDepth;
            }

            // Set inline CSS text properties.
            var labelColor = determineLabelColor(node.data.$color);
            if (node._depth === lowestDepth) { // The lowest depth shows as much text as possible.
                $('#' + domElement.id).removeClass('title-label').css({ 'color': labelColor });
            }
            else { // All other levels should condense to prevent text overlapping.
                $('#' + domElement.id).addClass('title-label').css({ 'color': labelColor });
            }
        }
    });

    // Some functions to run after initialization.
    setDropDowns(); // Set the depth / label options to display.
    getDataInfo(); // For title coloring.
    fullRefresh(0); // Loads the data and sets colors.
    setToolbar(isToolbarHidden); // Also adjusts the size of the canvas.
    setLayout(layoutType); // Necessary due to how the constructor works.
    initCrumbTrail(); // Reset the breadcrumb trail.
}

// Get the depth of the data from the root node's id so title colors/labels can be updated properly.
function getDataInfo() {
    dataDepth = Number(json.id);
}

// Handle some special characters for highlighting nodes.
function HtmlEncode(string) {
    return string.replace(/&/g, '&amp;').replace(/>/g, '&gt;').replace(/</g, '&lt;');
}

// Creates the HTML code for a tooltip row.
function TooltipRowHtml(left, right) {
    return '<tr><td><b>' + left + '</b></td><td>' + right + '</td></tr>';
}

// Gets the name of a data type for the tooltip.
function getTypeFromPrefix(prefix) {
    switch (prefix) {
        case 'q':
            return 'Quarter';
        case 's':
            return 'Sprint';
        case 't':
            return 'Team';
        case 'p':
            return 'Product';
        case 'h':
            return 'Theme';
        case 'b':
            return 'Product Backlog Item';
        default:
            return 'N/A';
    }
}

// Gets the color of a state for the tooltip.
function getColorFromState(state) {
    switch (state) {
        case 'New':
            return '#0077FF';
        case 'Approved':
            return '#BB33CC';
        case 'Committed':
            return '#FF6600';
        case 'Done':
            return '#00BB00';
        case 'Removed':
            return '#CC2200';
        default:
            return '#FFFFFF';
    }
}
/* <-----------------------END---- TREEMAP CREATION ------------------------------> */


/* <----------------------BEGIN--- TREEMAP MODIFICATION ------------------------------> */
// Entering a node.
function enterNode(node) {
    if (!treemap.busy && node._depth !== currentDepth) {
        // Track the progress.
        currentNode = node;
        currentDepth = node._depth;

        // Update depth dependent elements.
        setDropDowns(dataLevels.slice(currentDepth));
        updateColors(currentDepth);
        updateCrumbs(node);

        // Actually enter.
        treemap.enter(node);
    }
}

// Exiting a node, really backing out a level.
function exitNode() {
    if (!treemap.busy && currentDepth > 0) {
        // Track the progress.
        currentNode = currentNode.getParents()[0];

        // Update depth dependent elements.
        setDropDowns(dataLevels.slice(--currentDepth));
        updateColors(currentDepth);
        updateCrumbs(currentNode);

        // Actually exit.
        treemap.out();
    }
}

// Give the treemap nodes a new layout.
function setLayout(layoutType) {
    // Set the global reference.
    this.layoutType = layoutType;

    // Set the treemap settings.
    if (layoutType === 'row') {
        $jit.util.extend(treemap, new $jit.Layouts.TM.SliceAndDice);
        treemap.layout.orientation = 'h';
    }
    else if (layoutType === 'column') {
        $jit.util.extend(treemap, new $jit.Layouts.TM.SliceAndDice);
        treemap.layout.orientation = 'v';
    }
    else {
        $jit.util.extend(treemap, new $jit.Layouts.TM.Squarified);
    }

    // Refresh the treemap.
    treemap.refresh();
}

// Uses the node's color to determine a high contrast text label color.
function determineLabelColor(color) {
    // Parse the input.
    var redHex = color.slice(1, 3);
    var greenHex = color.slice(3, 5);
    var blueHex = color.slice(5, 7);
    var red = parseInt(redHex, 16);
    var green = parseInt(greenHex, 16);
    var blue = parseInt(blueHex, 16);

    // Magic numbers for determining a color's brightness.
    var brightness = Math.sqrt(.241 * red * red + .691 * green * green + .068 * blue * blue);

    // 50/50 split between white and black.
    var newColor = brightness < 128 ? 'white' : 'black';
    return newColor;
}

// Update the title bars to be specific colors.
function updateColors(currentDepth) {
    // Determine the lowest level to ignore coloring.
    var ignoreDepth;
    if (currentDepth + levelsToShow <= dataDepth) {
        ignoreDepth = currentDepth + levelsToShow;
    }
    else {
        ignoreDepth = dataDepth;
    }

    // Update each node's color depending on the current tree's depth.
    // Would be nice if this didn't require nodes to be built first to avoid refresh.
    treemap.graph.eachNode(function (node) {
        // The title colors from highest level to lowest.
        //var titleColors = ['#222222', '#444444', '#666666', '#888888'];
        var titleColors = ['#FFFFFF'];

        // Determine the node's depth by counting the dashes in its ID.
        var nodeDepth = node.id.match(/-/g) === null ? 0 : node.id.match(/-/g).length;

        // Make all title nodes and nodes without colors use specific colors.
        if (nodeDepth < ignoreDepth || node.data.$color === '') {
            // Back up the original color if it isn't used in the title colors.
            if ($.inArray(node.data.$color, titleColors) === -1) {
                node.data.oldColor = node.data.$color;
            }

            // Determine the title color.
            var color = titleColors[nodeDepth];
            if (color === undefined) { // If node depth > amount of colors...
                // ...make it the same color as the last one available.
                color = titleColors[titleColors.length - 1];
            }

            // Assign the appropriate title color.
            node.data.$color = color;
        }
        // Don't color non-title nodes. If oldColor exists, assign it back.
        else if (node.data.oldColor) {
            node.data.$color = node.data.oldColor;
        }
    });

    // Prevents graphical oddities, i.e. changing the depth setting.
    treemap.refresh();
}

// Sometimes a full refresh of the treemap is required.
function fullRefresh(currentDepth) {
    // Set the global reference.
    this.currentDepth = currentDepth;

    // Reset the node highlighting array.
    highlightNodes = {};

    // Reload the data and refresh to rebuild our nodes.
    treemap.loadJSON(json);
    treemap.refresh();

    // Update title colors now that nodes are built.
    updateColors(currentDepth);
}
/* <-----------------------END---- TREEMAP MODIFICATION ------------------------------> */


/* <----------------------BEGIN--- TREEMAP IMAGE SAVING ------------------------------> */
// Combines the treemap's canvas with the treemap's non-canvas style attributes to create an image file.
function generateImage() {
    var domElement = document.getElementById('treemap');
    html2canvas(domElement, {
        background: domElement.style.backgroundColor, // Seems like this should be the default.
        onrendered: function (canvas) {
            showDownload(canvas);
        }
    });
}

// Give a user-friendly way to download the canvas.
function showDownload(canvas) {
    // Convert the canvas to a data URI scheme.
    var dataUrl = canvas.toDataURL();

    // Check if the HTML5 download attribute is supported.
    var linkText;
    if ('download' in $('#export-link')[0]) {
        // Supported, just make a hyperlink.
        linkText = 'Download';
    }
    else {
        // Not supported, trigger the browser's save/view dialog.
        openBrowserDialog(dataUrl);
        linkText = 'View Image';
    }

    // Make the hyperlink in the dialog.
    $('#export-link').attr({ href: dataUrl, download: getFileName() }).text(linkText);
}

// Saving client-side generated files in web browsers is not as smooth as it should be so do this server-side nonsense.
function openBrowserDialog(dataUrl) {
    // Trim off the non-base64 parts of the data URI for server-side conversion to binary data.
    var base64 = dataUrl.slice(dataUrl.indexOf(',') + 1);

    // Trigger the browser save/view dialog in a user-friendly manner using a form.
    var base64Input = document.createElement('input');
    base64Input.setAttribute('name', 'base64');
    base64Input.setAttribute('value', base64);
    var filenameInput = document.createElement('input');
    filenameInput.setAttribute('name', 'filename');
    filenameInput.setAttribute('value', getFileName());
    var downloadForm = document.createElement('form');
    downloadForm.method = 'post';
    downloadForm.action = 'Request/GetPng';
    downloadForm.appendChild(base64Input);
    downloadForm.appendChild(filenameInput);
    document.body.appendChild(downloadForm);
    downloadForm.submit();
    document.body.removeChild(downloadForm);
}

// Determine the filename for the generated image.
function getFileName() {
    return treemapName === '' ? 'treemap.png' : treemapName + '.png';
}
/* <-----------------------END---- TREEMAP IMAGE SAVING ------------------------------> */


/* <----------------------BEGIN--- TREEMAP SAVING/LOADING ------------------------------> */
// Shows the available saves to load in the load dialog.
function initLoadTree() {
    // Disable the buttons.
    disableButton('#tree-delete,#tree-load');
    
    // Destroy.
    $('#jstree-load').jstree('destroy');
    
    // Rebuild.
    $('#jstree-load')
    .bind('loaded.jstree', function () {
        // Remove the space for jsTree icons.
        $('#jstree-load .jstree-leaf > ins').remove();

        // Make the anchors function like buttons.
        $('#jstree-load a').removeAttr('href')
        .click(function () {
            enableButton('#tree-delete,#tree-load');
        })
        .dblclick(function () {
            // Request a tree load.
            loadState();
        });
    })
    .jstree({
        'json_data': {
            'ajax': { // Would like to use jQuery's .always(), but jsTree has its own ajax thing.
                'url': 'Request/GetSaves',
                'error': function () {
                    networkError('Try opening the load menu again.');
                }
            }
        },
        'ui': {
            'select_limit': 1
        },
        'themeroller': { // Change the default icon.
            item_leaf: 'ui-icon-image'
        },
        plugins: ['json_data', 'ui', 'themeroller']
    });
}

// Requests a state to be deleted and reloads the tree of saves.
function deleteState() {
    // Prevent multiple delete requests.
    if (!isDeleteDone) {
        return;
    }

    // Get the selected node.
    var node = $('#jstree-load').jstree('get_selected');

    // Grab the name of the single entry.
    var name = node[0].textContent.substr(1);

    // Begin delete request.
    isDeleteDone = false;

    $.ajax({
        type: 'POST',
        url: 'Request/DeleteSave',
        data: { 'name': name },
        success: function () {
            // Refresh the entire tree of saves.
            initLoadTree();
        }
    })
    .always(function () {
        // End delete request.
        isDeleteDone = true;
    });
}

// Rebuilds the treemap using a saved state.
function loadState() {
    var node = $('#jstree-load').jstree('get_selected');

    // Get the selected tree's settings.
    var settings = node.data();

    // Store the treemap's name globally, ignore the leading space.
    treemapName = node[0].textContent.substr(1);

    // Update the building table's settings.
    updateTable(settings);

    // Save the filter and update the UI.
    setFilter(settings.filter);

    // Set the dropdown defaults.
    levelsToShow = Number(settings.level);
    labelsToShow[1] = Number(settings.label);

    // Set the treemap layout type.
    prepareLayout(settings.layout);

    // Set the velocity flag.
    setVelocityFlag(settings.velocity);
    
    // Request a new tree using these settings.
    requestTreeBuild();

    // Close the load dialog regardless of failure.
    closeLoadDialog();
}

// Loads the saved building table settings.
function updateTable(settings) {
    // Get the old build settings and order.
    var rows = slickGrid.getData();

    // Get the new build settings.
    var types = undefined, colors, sizes, sorts;
    if (settings.type !== '') { // Special case with no data selected.
        types = settings.type.split('-');
        colors = settings.$color.split('-');
        sizes = settings.size.split('-');
        sorts = settings.sort.split('-');
    }

    // Set the new build settings and order.
    var selectedRows = [];
    $.each(types, function (newIndex, type) {
        // Get the saved attributes for each type.
        var newColor = colors[newIndex];
        var newSize = sizes[newIndex];
        var newSort = sorts[newIndex];

        // Iterate through the rows in the table until the right row is found.
        var oldIndex;
        var rowToMove;
        $.each(rows, function (rowIndex, row) {
            if (row.group === type) {
                // Overwrite the row's old attributes with the new attributes.
                row.color = newColor;
                row.size = newSize;
                row.sort = newSort;

                // Save the index and row.
                oldIndex = rowIndex;
                rowToMove = row;

                // Stop iterating.
                return;
            }
        });

        // If it needs to swap and it's movable...
        if (newIndex !== oldIndex && rowToMove.order !== 'N/A') {
            // ...swap the rows, making a shallow copy.
            var otherRow = $.extend({}, rows[newIndex]);
            rows[newIndex] = rowToMove;
            rows[oldIndex] = otherRow;

            // Row moved so use the new index to select the row.
            selectedRows.push(newIndex);
        }
        else {
            // Row didn't move so use the old index to select the row.
            selectedRows.push(oldIndex);
        }
    });

    // Update the building table with the new settings.
    slickGrid.setData(rows);
    slickGrid.setSelectedRows(selectedRows);
    slickGrid.render();
}

// Sets the global layout type and updates the button UIs.
function prepareLayout(layoutType) {
    this.layoutType = layoutType;

    // Fire a change event on each button to update its state.
    $('#layout-buttons input').each(function () {
        if (this.id === layoutType) {
            $(this).change();
        }
    });
}

// Sets the UI for displaying misesimated nodes.
function setVelocityFlag(isFlag) {
    // Handle a string or a boolean being passed in.
    var isVelocityFlag = String(isFlag) === 'true';

    // Set the global reference.
    this.isVelocityFlag = isVelocityFlag;

    // Update the checkbox.
    $('#velocity').prop('checked', isVelocityFlag);
}

// Determine and save the treemap's state.
function saveState() {
    // Get the value from the textbox.
    var saveText = $('#save-name').val();

    // Prevent multiple save requests and using an empty save name.
    if (!isSaveDone || saveText === '') {
        return;
    }

    // The name was changed so be prepared to reconfirm if the save name exists.
    if (confirmedSave && lastFailedSaveName !== saveText) {
        confirmedSave = false;
    }

    // Begin save request.
    isSaveDone = false;
    disableButton('#tree-save');

    $.ajax({
        type: 'POST',
        url: 'Request/SaveTree',
        data: determineState(),
        dataType: 'text',
        success: function (message) {
            // The save name already exists.
            if (message === 'exists') {
                confirmSave(saveText);
            }
            else {
                // Save a global reference.
                treemapName = saveText;

                // Update the first node to show the name of the saved treemap.
                $('#breadcrumb-list li:first span').next().text(saveText);

                // Close the dialog.
                closeSaveDialog();
            }
        }
    })
    .always(function () {
        // End save request.
        isSaveDone = true;
        enableButton('#tree-save');
    });
}

// Currently ignores the active node/node path and the screen size setting.
function determineState() {
    return determineTreeBuild()
    + '&level=' + levelsToShow
    + '&label=' + labelsToShow[1]
    + '&layout=' + layoutType
    + '&name=' + $('#save-name').val()
    + '&velocity=' + isVelocityFlag
    + "&overwrite=" + confirmedSave;
}

// Confirm if the save should overwrite an existing one.
function confirmSave(name) {
    // Set the global variables.
    confirmedSave = true;
    lastFailedSaveName = name;

    // Populate the error message.
    $('#save-error-message').text("'" + name + "'" + ' is taken. Save again to overwrite the existing save.');

    // Bring focus to the save name field.
    $('#save-name').focus();
}

// Save the state when the enter key is used on the textbox.
function handleSaveKeys(event) {
    if (event.which === 13) { // 13 = enter key.
        saveState();
    }
    else {
        updateSaveButton();
    }
}

// Updates the save button's state according to the save name field's contents.
function updateSaveButton() {
    if ($('#save-name').val() === '') {
        disableButton('#tree-save');
    }
    else {
        enableButton('#tree-save');
    }
}
/* <-----------------------END---- TREEMAP SAVING/LOADING ------------------------------> */


/* <----------------------BEGIN--- DATA LOADING ------------------------------> */
// Showing/hiding elements when treemap loading begins.
function startTreemapLoading() {
    // Hide the containers.
    if (!isToolbarHidden) {
        $('#toolbar').hide();
    }
    $('#main').hide();

    // Update the loading message.
    var formattedName = treemapName;
    if (treemapName !== '') {
        formattedName = "'" + treemapName + "'";
    }
    $('#loading-message').html('Loading ' + formattedName + '. . .');

    // Activate the loading screen centered on the page.
    var height = $('#loading').outerHeight();
    var width = $('#loading').outerWidth();
    $('#loading').css({
        top: '50%',
        left: '50%',
        marginTop: height / -2,
        marginLeft: width / -2
    }).show();
}

// Showing/hiding elements when treemap loading ends.
function stopTreemapLoading() {
    // Show the containers.
    if (!isToolbarHidden) {
        $('#toolbar').show();
    }
    $('#main').show();

    // Deactivate the loading screen.
    $('#loading').hide();
}

// Handles fetching the treemap data.
function requestTreeBuild() {
    // Prevent multiple build requests.
    if (!isTreeRefreshDone) {
        return;
    }

    // Begin build request.
    isTreeRefreshDone = false;
    startTreemapLoading();

    // Must be POST due to possibly exceeding GET character limit.
    $.ajax({
        type: 'POST',
        url: 'Request/GetTree',
        data: determineTreeBuild(),
        dataType: 'json',
        success: function (json) {
            // Remove the old treemap (if it exists) and create a new one.
            removeTree();
            buildTree(json);
        },
        error: function () {
            networkError('Try drawing the treemap again.');
            removeTree();
            hideToolbarElements();
        }
    })
    .always(function () {
        // End build request.
        stopTreemapLoading();
        isTreeRefreshDone = true;
    });
}

// Create a new treemap.
function buildTree(json) {
    // Show nothing if nothing is selected.
    if (json.length === 0) {
        clearTrail();
        hideToolbarElements();
    }
    else {
        // Inject a new treemap.
        initTreeMap(json);
        showToolbarElements();
    }
}

// Remove the treemap injection, which only exists after the first build.
function removeTree() {
    $('#treemap-canvaswidget').remove();
    treemap = undefined;
}

// Show only toolbar elements that make sense.
function showToolbarElements() {
    $('.toolbar-hidable').show();
    enableMenuButton('#toolbar-save,#toolbar-image');
}

// Hide toolbar elements at start or when it doesn't make sense.
function hideToolbarElements() {
    $('.toolbar-hidable').hide();
    disableMenuButton('#toolbar-save,#toolbar-image');
}

// Determine the type and order of data to build.
function determineTreeBuild() {
    // Build the POST.
    var data = slickGrid.getData();
    var selectedRows = slickGrid.getSelectedRows().sort();
    dataType = '';
    var color = '&color=';
    var size = '&size=';
    var sort = '&sort=';
    $.each(selectedRows, function () {
        dataType += data[this].group + levelSplitter;
        color += data[this].color + levelSplitter;
        size += data[this].size + levelSplitter;
        sort += data[this].sort + levelSplitter;
    });

    // Remove the final splitters.
    dataType = dataType.substring(0, dataType.length - 1);
    color = color.substring(0, color.length - 1);
    size = size.substring(0, size.length - 1);
    sort = sort.substring(0, sort.length - 1);

    return 'type=' + dataType + color + size + sort + '&filter=' + getFilter();
}
/* <-----------------------END---- DATA LOADING ------------------------------> */


/* <----------------------BEGIN--- DATA BUILDING ------------------------------> */
// Initialize the data select/order table.
function initSlickGrid() {
    // Override some default options.
    var options = {
        autoHeight: true,
        editable: true,
        enableColumnReorder: false,
        rowHeight: 30
    };

    // Define the rows.
    var rows = [makeRow('Quarter'), makeRow('Sprint'), makeRow('Team'), makeRow('Theme'), makeRow('Product'), makeRow('PBI')];

    // Define the columns.
    var checkboxSelector = new Slick.CheckboxSelectColumn({
        cssClass: 'cell-select',
        toolTip: 'Select/Deselect All Groupings'
    });
    var columns = initColumnSettings(checkboxSelector);

    // Create the grid.
    slickGrid = new Slick.Grid('#slick-grid', rows, columns, options);
    slickGrid.setSelectionModel(new Slick.RowSelectionModel({ selectActiveRow: false }));
    slickGrid.registerPlugin(checkboxSelector);

    // Define some row behaviors.
    initRowSelection();
    initRowMovement();
    initRowEditing();

    // By default, select all the rows except Quarter/Product.
    var selectedRows = [];
    for (var j = 0; j < slickGrid.getDataLength(); j++) {
        if (j != 0 && j != 4) {
            selectedRows.push(j);
        }
    }
    slickGrid.setSelectedRows(selectedRows);

    // Change the cursor style for the last row.
    slickGrid.addCellCssStyles(null, {
        5: {
            group: 'unmovable-cell',
            order: 'unmovable-cell'
        }
    });
}

// Makes a row of data for the treemap building table.
function makeRow(rowType) {
    // Make a new row and set its name.
    var row = {};
    row.group = rowType;

    // Set the defaults for the row.
    // Some rows override the default column options.
    switch (rowType) {
        case 'Quarter':
            row.color = 'Name';
            row.size = 'Nothing';
            row.sort = 'Date';
            row.sortOptionsOverride = 'Date,Effort';
            break;
        case 'Sprint':
            row.color = 'Name';
            row.size = 'Nothing';
            row.sort = 'Date';
            row.sortOptionsOverride = 'Date,Effort';
            break;
        case 'Team':
            row.color = 'Name';
            row.size = 'Aligning';
            row.sort = 'Product';
            row.sortOptionsOverride = 'Alphabet,Effort,Product';
            break;
        case 'Product':
            row.color = 'Name';
            row.size = 'Nothing';
            row.sort = 'Alphabet';
            row.sortOptionsOverride = 'Alphabet,Effort';
            break;
        case 'Theme':
            row.color = 'Program';
            row.colorOptionsOverride = 'Program';
            row.size = 'Nothing';
            row.sort = 'Alphabet';
            break;
        case 'PBI':
            row.color = 'State';
            row.colorOptionsOverride = 'State';
            row.size = 'Nothing';
            row.sizeOptionsOverride = 'Nothing,Effort';
            row.sort = 'Alphabet';
            break;
    }

    return row;
}

// Define the defaults for column data.
function initColumnSettings(checkboxSelector) {
    return [checkboxSelector.getColumnDefinition(),
    {
        id: 'group',
        name: 'Grouping',
        field: 'group',
        width: 80,
        behavior: 'move',
        resizable: false,
        toolTip: 'Drag Me to Reorder',
        cssClass: 'cell-move'
    },
    {
        id: 'order',
        name: 'Order',
        field: 'order',
        width: 50,
        behavior: 'move',
        resizable: false,
        toolTip: 'Drag Me to Reorder',
        cssClass: 'cell-move'
    },
    {
        id: 'color',
        name: 'Color by',
        field: 'color',
        width: 90,
        behavior: 'select',
        resizable: false,
        cssClass: 'cell-select',
        editor: Slick.Editors.Select,
        options: 'Name'
    },
    {
        id: 'size',
        name: 'Size by',
        field: 'size',
        width: 90,
        behavior: 'select',
        resizable: false,
        cssClass: 'cell-select',
        editor: Slick.Editors.Select,
        options: 'Nothing,Aligning,Effort'
    },
    {
        id: 'sort',
        name: 'Sort by',
        field: 'sort',
        width: 90,
        behavior: 'select',
        resizable: false,
        cssClass: 'cell-select',
        editor: Slick.Editors.Select,
        options: 'Alphabet,Effort,Priority'
    }];
}

// Define row selection logic and select all the rows by default.
function initRowSelection() {
    // Define row selection logic.
    slickGrid.onSelectedRowsChanged.subscribe(function (event, args) {
        var rowData = slickGrid.getData();
        var rows = args.rows;
        var ordering = 1;
        $.each(rowData, function (index, value) {
            // If it's selected, give it an ordering number.
            if ($.inArray(index, rows) !== -1) {
                if (value.group === 'PBI') {
                    this.order = 'N/A';
                }
                else {
                    this.order = ordering++;
                }
            }
            else {
                this.order = '';
            }
        });
        slickGrid.invalidate(); // Redraw the grid.

        // Disable inactive portions.
        $('.slick-cell').not('.selected').css({ 'color': '#888888' }).children('button').prop('disabled', true).addClass('ui-state-disabled');

        // Add tooltips to the movable cells.
        $('.cell-move').attr('title', 'Drag Me Up/Down to Reorder');
    });
}

// Define row moving logic and prevent the last row from moving.
function initRowMovement() {
    // Create the move row plugin.
    var moveRowsPlugin = new Slick.RowMoveManager();

    // Adds a thick line to indicate the row will actually move.
    moveRowsPlugin.onBeforeMoveRows.subscribe(function (e, data) {
        for (var i = 0; i < data.rows.length; i++) {
            // The last boolean prevents things from being moved underneath the last row.
            if (data.rows[i] == data.insertBefore || data.rows[i] == data.insertBefore - 1 || data.insertBefore == slickGrid.getDataLength()) {
                e.stopPropagation();
                return false; // Don't move.
            }
        }
        return true; // Move.
    });

    // Actual row movement.
    moveRowsPlugin.onMoveRows.subscribe(function (e, args) {
        var rowData = slickGrid.getData();

        // Copy-pasted from SlickGrid docs.
        var extractedRows = [], left, right;
        var rows = args.rows;
        var insertBefore = args.insertBefore;
        left = rowData.slice(0, insertBefore);
        right = rowData.slice(insertBefore, rowData.length);
        rows.sort(function (a, b) { return a - b; });
        for (var i = 0; i < rows.length; i++) {
            extractedRows.push(rowData[rows[i]]);
        }
        rows.reverse();
        for (var j = 0; j < rows.length; j++) {
            var row = rows[j];
            if (row < insertBefore) {
                left.splice(row, 1);
            } else {
                right.splice(row - insertBefore, 1);
            }
        }

        // Mine.
        var rowTypesSelected = [];
        $.each(slickGrid.getSelectedRows(), function () {
            rowTypesSelected.push(rowData[this].group);
        });

        // Copy-pasted from SlickGrid docs.
        rowData = left.concat(extractedRows.concat(right));

        // Mine.
        var selectedRows = [];
        $.each(rowData, function (index) {
            if ($.inArray(this.group, rowTypesSelected) != -1) {
                selectedRows.push(index);
            }
        });

        // Copy-pasted from SlickGrid docs.
        slickGrid.resetActiveCell();
        slickGrid.setData(rowData);
        slickGrid.setSelectedRows(selectedRows);
        slickGrid.render();
    });

    // Register the plugin now that it's configured.
    slickGrid.registerPlugin(moveRowsPlugin);
}

// Define row editing logic.
function initRowEditing() {
    // Create the editor on hover for the dropdown cells.
    slickGrid.onMouseEnter.subscribe(function (e, args) {
        if ($('.ui-selectmenu-open').length === 0) {
            editActiveCell(e, args);
        }
    });

    // Destroy the editor on mouse leave for the dropdowns.
    slickGrid.onMouseLeave.subscribe(function (e) {
        // The relatedTarget check is for dealing with slickgrid's way of entering 'edit mode'.
        if (e.relatedTarget !== undefined && $('.ui-selectmenu-open').length === 0) {
            slickGrid.resetActiveCell();
        }
    });

    // Clicking on a dropdown cell while dropdown cell menu is already open requires this.
    slickGrid.onClick.subscribe(function (e, args) {
        editActiveCell(e, args);
    });

    // Prevent editing in unselected rows.
    slickGrid.onBeforeEditCell.subscribe(function (e, args) {
        if ($.inArray(args.row, slickGrid.getSelectedRows()) === -1) {
            return false;
        }
        return true;
    });
}

// Show the editor for the active cell in the data building table.
function editActiveCell(e, args) {
    // Get the cell and create the editor.
    var cell = slickGrid.getCellFromEvent(e);
    slickGrid.setActiveCell(cell.row, cell.cell);
    slickGrid.editActiveCell();
    var editor = slickGrid.getCellEditor();

    // Change the editor to a jQuery UI type.
    var column = args.grid.getColumns()[cell.cell];
    $('#edit-select').selectmenu({ width: column.width - 3 })
    .change(function () {
        editor.applyValue(slickGrid.getDataItem(cell.row), e.target.firstChild.value);
        editor.destroy();
        slickGrid.resetActiveCell();
    });
}
/* <-----------------------END---- DATA BUILDING ------------------------------> */


/* <----------------------BEGIN--- DATA FILTERING ------------------------------> */
// Initialize the filtering tree.
function initFilterTree() {
    $('#jstree-filter').bind('loaded.jstree', function () {
        // Make the anchors function like buttons.
        $('#jstree-filter a').each(function () {
            // Replace the location attribute with an onclick event.
            $(this).removeAttr('href').attr('title', 'Enable/Disable Grouping').click(function (e) {
                if (!e.target.classList.contains('jstree-checkbox')) {
                    var node = this.parentElement;
                    if ($('#jstree-filter').jstree('is_checked', node)) {
                        $('#jstree-filter').jstree('uncheck_node', node);
                    }
                    else {
                        $('#jstree-filter').jstree('check_node', node);
                    }
                }
            });
        });

        // Select all the checkboxes.
        $('#jstree-filter').jstree('check_all');
    })
    .jstree({
        'json_data': {
            'ajax': { // Would like to use jQuery's .always(), but jsTree has its own ajax thing.
                'url': 'Request/GetFilter',
                'error': function () {
                    networkError('Try refreshing the page.');
                }
            }
        },
        'themeroller': { // Remove the default icons.
            item_open: false,
            item_clsd: false,
            item_leaf: false
        },
        plugins: ['json_data', 'checkbox', 'themeroller']
    });
}

// Gets the selected groupings to filter out.
function getFilter() {
    // For each filter level.
    var groupingFilter = '';
    $('#jstree-filter > ul > li').each(function () {
        // Get the name of the group.
        var levelNode = this;
        var groupingName = $('#jstree-filter').jstree('get_text', levelNode);

        // Check each row in slickGrid for a matching group.
        $.each(slickGrid.getData(), function () {
            if (this.group === groupingName) {
                // Set the name of the group as the first entry.
                groupingFilter += groupingName;

                // Determine what nodes to examine for checkmarks.
                var filterNodes = levelNode;
                if (groupingName === 'Theme') { // Themes have an extra nested level.
                    filterNodes = $(levelNode).children('ul').children('li');
                }

                // Get the selected filters.
                $('#jstree-filter').jstree('get_checked', filterNodes).each(function () {
                    var childName = encodeURIComponent($('#jstree-filter').jstree('get_text', this)); // Encode some special characters.
                    groupingFilter += '\t' + childName;
                });

                // Add a level separator after each level except the last.
                var levelSeparator = '\t' + levelSplitter + '\t';
                if (!$(levelNode).hasClass('jstree-last')) {
                    groupingFilter += levelSeparator;
                }

                // Stop looking.
                return;
            }
        });
    });
    return groupingFilter;
}

// Sets the filter tree UI using a built filter string. Used for loading saved filters.
function setFilter(filter) {
    // Map each grouping to an array for filtering.
    var groupingFilters = {};
    var groupings = filter.split('\t-\t');
    $.each(groupings, function () {
        var filterNames = this.split('\t');
        var grouping = filterNames.splice(0, 1); // The first entry is the grouping type.
        groupingFilters[grouping] = filterNames;
    });

    // For each top-level grouping...
    $('#jstree-filter > ul > li').each(function () {
        // ...get the mapped array for filtering on.
        var node = this;
        var grouping = $('#jstree-filter').jstree('get_text', node);
        var groupingFilter = groupingFilters[grouping];

        // For each child node on the grouping...
        var childrenNodes = $(this).children('ul').children('li');
        if (grouping === 'Theme') { // Themes have an extra nested level.
            childrenNodes = $(childrenNodes).children('ul').children('li');
        }

        $(childrenNodes).each(function () {
            // ...determine its check state if its in the filter array.
            node = this;
            var nodeName = $('#jstree-filter').jstree('get_text', node);
            if ($.inArray(nodeName, groupingFilter) !== -1) {
                $('#jstree-filter').jstree('check_node', node);
            }
            else {
                $('#jstree-filter').jstree('uncheck_node', node);
            }
        });
    });

    // jsTree opens changed nodes by default so close all of them.
    $('#jstree-filter').jstree('close_all', $('#jstree-filter'), false);
}
/* <-----------------------END---- DATA FILTERING ------------------------------> */


/* <----------------------BEGIN--- DYNAMIC DROPDOWNS ------------------------------> */
// Set the depth level. Requires a full refresh of data due to title coloring.
function setDepth(levelsToShow) {
    // Set the global reference.
    this.levelsToShow = Number(levelsToShow);

    // Update the treemap setting.
    treemap.config.levelsToShow = this.levelsToShow;

    // Update the dropdowns.
    setDropDowns(dataLevels.slice(currentDepth, levelsToShow + 1));

    // Refresh the treemap.
    fullRefresh(currentDepth);
}

// Set the depth of labels to show with respect to the current depth.
function setLabels(labelsToShow) {
    // Set the global reference.
    this.labelsToShow = [0, Number(labelsToShow)];

    // Update the treemap setting.
    treemap.config.labelsToShow = this.labelsToShow;

    // Refresh the treemap.
    treemap.refresh();
}

// Set the dropdown selection options.
function setDropDowns(levels) {
    // If nothing is passed in, then dataLevels needs to be initialized.
    if (levels === undefined) {
        dataLevels = dataType.split(levelSplitter);
        levels = dataLevels;
    }

    // Remove the old entries for the depth dropdown.
    var dropdown = document.getElementById('depth-setting');
    for (var i = dropdown.length - 1; i >= 0; i--) {
        dropdown.remove(i);
    }

    // Create the new entries for the depth dropdown.
    var option;
    var selectedIndex = -1; // Used for determining the entries for the label dropdown.
    for (var j = 0; j < levels.length; j++) {
        option = document.createElement('option');
        option.text = levels[j];
        option.value = j + 1;
        if (option.value == levelsToShow) { // If it matches their last 'set' setting, use that for navigational consistency.
            option.selected = 'selected';
            selectedIndex = j;
        }
        if (j === levels.length - 1 && selectedIndex === -1) { // If their 'set' setting is too high, use the next best thing.
            option.selected = 'selected';
            selectedIndex = j;
        }
        dropdown.add(option);
    }

    // Get the last level as the only option since there weren't any valid options.
    var options = dropdown.options;
    if (options.length === 0) {
        option = document.createElement('option');
        option.text = dataLevels[dataLevels.length - 1];
        dropdown.add(option);
        selectedIndex = 0;
    }

    // Remove the old entries for the label dropdown.
    dropdown = document.getElementById('label-setting');
    for (var k = dropdown.length - 1; k >= 0; k--) {
        dropdown.remove(k);
    }

    // Create the new entries for the label dropdown using the depth dropdown entries as a base.
    var defaultSelected = false;
    for (var m = 0; m <= selectedIndex; m++) {
        option = document.createElement('option');
        option.text = options[m].text;
        option.value = m + 1;
        if (option.value == labelsToShow[1]) { // If it matches their last 'set' setting, use that for navigational consistency.
            option.selected = 'selected';
            defaultSelected = true;
        }
        if (m === selectedIndex && defaultSelected === false) { // If their 'set' setting is too high, use the next best thing.
            option.selected = 'selected';
        }
        dropdown.add(option);
    }

    // (re)Build the jQuery UI elements.
    $('#depth-setting,#label-setting').selectmenu({ 'width': 75 });
}
/* <-----------------------END---- DYNAMIC DROPDOWNS ------------------------------> */


/* <----------------------BEGIN--- SIZING ------------------------------> */
// Trigger UI elements when switching size modes.
function setFullScreen(isFullScreen) {
    // Don't bother if the setting is already set.
    if (this.isFullScreen === isFullScreen) {
        return;
    }

    // Set the global value for reference.
    this.isFullScreen = isFullScreen;

    // Toggle whether the UI elements are enabled or disabled.
    toggleSizeUI();

    // Focus and select the first box when switching to custom.
    if (!isFullScreen) {
        $('#width-box').focus().select();
    }

    // Update the page properties, resizing the canvas if necessary.
    resize();
}

// Used for limiting input in the width/height boxes.
function isNumber(event) {
    var charCode = event.which;

    // Initiate a resize on enter.
    if (charCode === 13) {
        resize();
        return false;
    }

    // Allow control chars and numbers.
    if (charCode > 31 && (charCode < 48 || charCode > 57))
        return false;
    return true;
}

// jQuery event handler for browser resizing.
$(window).resize(function () {
    // Update the treemap sizes if it's not currently being built.
    if (isTreeRefreshDone) {
        resize();
    }

    // Update the tree menu size limits.
    resizeTreeMenus();
});

// Prevent the tree menus from overexpanding their dialogs.
function resizeTreeMenus() {
    $('#jstree-filter').css({ 'maxHeight': window.innerHeight - aboveFilterTree });
    $('#jstree-load').css({ 'maxHeight': window.innerHeight - aboveLoadTree });
}

// Handle resizing the canvas.
function resize() {
    // Define the size of the canvas.
    var widthBox = document.getElementById('width-box');
    var heightBox = document.getElementById('height-box');
    var toolbarWidth = isToolbarHidden ? 0 : $('#toolbar').outerWidth();
    var availableWidth = window.innerWidth - toolbarWidth;
    var availableHeight = window.innerHeight - breadcrumbHeight;
    var canvasWidth, canvasHeight;
    if (isFullScreen) { // Get the values from the screen.
        $('body').css('position', 'fixed'); // Bug fix: prevents scrollbars from showing when treemap breaks the canvas.
        canvasWidth = availableWidth;
        canvasHeight = availableHeight;
        widthBox.value = canvasWidth;
        heightBox.value = canvasHeight;
    }
    else { // Get the values from the boxes.
        $('body').css('position', ''); // Bug fix: turn scrollbars back on.

        var widthBoxValue = Number(widthBox.value);
        var heightBoxValue = Number(heightBox.value);

        // The upper limit of 8192 will work in Firefox. Other browsers have no set limit, but are unreliable.
        if (widthBoxValue > 8192) {
            widthBox.value = 8192;
            widthBoxValue = widthBox.value;
        }
        if (heightBoxValue > 8192) {
            heightBox.value = 8192;
            heightBoxValue = heightBox.value;
        }

        canvasWidth = widthBoxValue;
        canvasHeight = heightBoxValue;
    }

    // Center the canvas horizontally if it's smaller than the space available.
    var marginLeft = (availableWidth - canvasWidth) / 2;
    if (marginLeft > 0) {
        $('#main').css({ 'marginLeft': marginLeft });
    }
    else {
        $('#main').css({ 'marginLeft': '' });
    }

    // Center the canvas vertically if it's smaller than the space available.
    var marginTop = (availableHeight - canvasHeight) / 2;
    if (marginTop > 0) {
        $('#main').css({ 'marginTop': marginTop });
    }
    else {
        $('#main').css({ 'marginTop': '' });
    }

    // Only resize the canvas if needed.
    if (treemap !== undefined) {
        var currentSize = treemap.canvas.getSize();
        if (currentSize.width !== canvasWidth || currentSize.height !== canvasHeight) {
            $('body').css({ 'width': canvasWidth + toolbarWidth, 'height': canvasHeight + breadcrumbHeight }); // Update the body's size to prevent bad scrollbars.
            $('#main').css({ 'width': canvasWidth, 'height': canvasHeight + breadcrumbHeight }); // Set the background.
            treemap.canvas.resize(canvasWidth, canvasHeight); // Set the visualization.
        }
    }
}

// Toggle the size-related UI elements to be enabled or disabled.
function toggleSizeUI() {
    $('#size-table input').prop('disabled', isFullScreen).toggleClass('ui-state-disabled');
    $('#size-table label').toggleClass('ui-state-disabled');
    $('#tree-resize').button('option', 'disabled', isFullScreen);
}
/* <-----------------------END---- SIZING ------------------------------> */


/* <----------------------BEGIN--- TOOLBAR TOGGLE ------------------------------> */
// Enable/disable the side toolbar visibility.
function setToolbar(isToolbarHidden) {
    // Set the global reference.
    this.isToolbarHidden = isToolbarHidden;

    if (isToolbarHidden) {
        // Override the inline css properties.
        $('#toolbar').hide().css({ 'width': 0 });
        $('#main').css({ 'left': 0 });
    }
    else {
        // Remove the inline override if it exists.
        $('#toolbar').show().css({ 'width': '' });
        $('#main').css({ 'left': '' });
    }
    resize();
}

// jQuery event handler for hiding the toolbar.
$(document).keypress(function (e) {
    if (e.altKey !== true && e.ctrlKey !== true && e.metaKey !== true && e.which === 103 && !$('#save-dialog').is(':visible')) { // 103 = lowercase g by itself.
        setToolbar(!isToolbarHidden);
    }
});
/* <-----------------------END---- TOOLBAR TOGGLE ------------------------------> */


/* <----------------------BEGIN--- BREADCRUMB TRAIL ------------------------------> */
// Clear the trail and make the first crumb.
function initCrumbTrail() {
    clearTrail();
    addRoot();
}

// Reset the state of the trail.
function clearTrail() {
    nodePath = [];
    $('#breadcrumb-list').empty();
}

// The root node is never removed until the trail is cleared.
function addRoot() {
    var rootNode = treemap.graph.getNode(treemap.root);
    addCrumb(rootNode);
}

// Update the crumb trail and its properties.
function updateCrumbs(node) {
    // Remove some UI elements for the half second the active crumb visible.
    $('#breadcrumb-list .ui-state-active').removeClass('ui-corner-tr ui-state-active');

    // Find the node's index in nodePath.
    var nodeIndex = -1;
    for (var nodePathIndex = 0; nodePathIndex < nodePath.length; nodePathIndex++) {
        if (node.id == nodePath[nodePathIndex].id) {
            nodeIndex = nodePathIndex;
            break;
        }
    }

    var activeCrumb;
    if (nodeIndex === -1) { // it's not in the trail, add it and any of its missing parents.
        // Get the node's parents in 'reverse' order by continously getting a parent.
        var parentNodes = [];
        var parentNode = node.getParents()[0];
        while (parentNode.id !== nodePath[nodePath.length - 1].id) { // Don't stop until we reach an existing parent crumb.
            parentNodes.push(parentNode);
            parentNode = parentNode.getParents()[0];
        }

        // Add the parents, unreversing to get the correct order.
        for (var parentIndex = parentNodes.length - 1; parentIndex >= 0; parentIndex--) {
            parentNode = parentNodes[parentIndex];
            addCrumb(parentNode);
        }

        // Push the final node.
        addCrumb(node);

        // Identify the active crumb.
        activeCrumb = $('#breadcrumb-list li').last();
    }
    else { // It's already in the trail, remove anything after it.
        removeCrumbsAfter(nodeIndex);

        // Identify the new active crumb.
        activeCrumb = $('#breadcrumb-list li').eq(nodeIndex);
    }

    // Set the active crumb and give every crumb besides the first a left border.
    activeCrumb.addClass('ui-corner-tr ui-state-active');
    $('#breadcrumb-list li:not(:first)').css({ 'borderLeft': '1px solid white' });
}

// Add crumbs when navigating forward.
function addCrumb(node) {
    // Create a crumb list item.
    var crumbListItem = $(document.createElement('li')).addClass('ui-state-default')
    .click(function () {
        enterNode(node);
    })
    .hover(
		function () {
		    $(this).addClass('ui-state-hover');
		},
		function () {
		    $(this).removeClass('ui-state-hover');
		}
	);

    // Create the crumb's label.
    var crumbName = $(document.createElement('span'));
    if (nodePath.length === 0) {
        // Use the treemap's name for the first crumb.
        crumbName.text(treemapName);

        // First crumb gets an icon.
        var crumbIcon = $(document.createElement('span'));
        crumbIcon.addClass('ui-icon ui-icon-home').css({ 'float': 'left' });
        crumbListItem.addClass('ui-corner-tl ui-corner-tr ui-state-active').append(crumbIcon);
    }
    else {
        // Use the node's name as the label.
        crumbName.text(node.name);
    }

    // Add it to the DOM with a fade in.
    $('#breadcrumb-list').append(crumbListItem.append(crumbName).hide().fadeIn(500));

    // Update the current path.
    nodePath.push(node);
}

// Remove crumbs when navigating backwards.
function removeCrumbsAfter(nodeIndex) {
    // Remove all crumbs after the index.
    $('#breadcrumb-list li')
    .each(function (listItemIndex) {
        // Fade out and then physically remove each affected crumb.
        if (listItemIndex > nodeIndex) {
            $(this).fadeOut(500, function () {
                $(this).remove();
            });
        }
    });
    
    // Update the current path.
    nodePath = nodePath.slice(0, nodeIndex + 1);
}
/* <-----------------------END---- BREADCRUMB TRAIL ------------------------------> */


/* <----------------------BEGIN--- JQUERY UI ------------------------------> */
// Prepare some jQuery UI widgets.
function initUIElements() {
    initMenu();
    initButtons();
    initLayoutButtons();
    initDialogs();
    initAccordion();
    $('#toolbar').show();
}

// Make the toolbar menu look nice and work.
function initMenu() {
    $('#toolbar-menu').menu({
        select: function (event, ui) {
            var id = ui.item[0].id;
            switch (id) {
                case 'toolbar-new':
                    openBuildDialog();
                    break;
                case 'toolbar-load':
                    openLoadDialog();
                    break;
                case 'toolbar-save':
                    openSaveDialog();
                    break;
                case 'toolbar-image':
                    openExportDialog();
                    generateImage();
                    break;
                case 'toolbar-help':
                    openHelpDialog();
                    break;
            }
        },
        blur: function () { // Bug fix: jQuery UI prevents the input from getting focus
            if ($('#save-dialog').dialog('isOpen')) {
                $('#save-name').focus();
            }
        }
    });
}

// Style elements to be buttons and button collections.
function initButtons() {
    // Convert the radio buttons into jQuery UI button sets.
    $('.radio-buttons').buttonset();

    // The button in the build dialog.
    makeIconButton('#tree-build', 'ui-icon-image');

    // The buttons in the load/save dialog.
    makeIconButton('#tree-load,#tree-save', 'ui-icon-check');

    // The button in the load dialog.
    makeIconButton('#tree-delete', 'ui-icon-close');

    // The button for setting the size.
    makeIconButton('#tree-resize', 'ui-icon-arrow-4-diag');
}

// Make a button with a single icon.
function makeIconButton(id, icon) {
    $(id).button({ icons: { primary: icon } });
}

// Prevents jQuery UI buttons from being used.
function disableButton(id) {
    $(id).button('option', 'disabled', true);
}

// Allows jQuery UI buttons to be used.
function enableButton(id) {
    $(id).button('option', 'disabled', false);
}

// Prevents jQuery UI menu options from being used.
function disableMenuButton(id) {
    $(id).addClass('ui-state-disabled');
}

// Allows jQuery UI menu options to be used.
function enableMenuButton(id) {
    $(id).removeClass('ui-state-disabled');
}

// Set the interaction for the layout button icons.
function initLayoutButtons() {
    // Add mouse enter/leave events for the hover state UI.
    $('#layout-buttons label').each(function () {
        $(this).hover(
        function () {
            if (!$(this).hasClass('ui-state-active')) {
                $(this.firstChild.firstElementChild).addClass('hover');
            }
        },
        function () {
            $(this.firstChild.firstElementChild).removeClass('hover');
        });
    });

    // Add the change event for the active state UI (would be nice if I got the prev/next instead of iterating).
    $('#layout-buttons input').each(function () {
        $(this).change(function () {
            var id = this.id;
            $('#layout-buttons label').each(function () {
                if (this.htmlFor === id) {
                    $(this).addClass('ui-state-active');
                    $(this.firstChild.firstElementChild).addClass('active');
                }
                else {
                    $(this).removeClass('ui-state-active');
                    $(this.firstChild.firstElementChild).removeClass('active');
                }
            });
        });
    });

    // Also set the correct one on startup.
    $('#layout-buttons label.ui-state-active').each(function () {
        $(this.firstChild.firstElementChild).addClass('active');
    });
}

// Create the jQuery UI dialogs.
function initDialogs() {
    // Make all of the dialogs on start.
    makeModalDialog('#build-dialog', 'New Treemap', 520);
    makeModalDialog('#load-dialog', 'Load Treemap', 400);
    makeModalDialog('#save-dialog', 'Save Treemap As...', 350);
    makeModalDialog('#export-dialog', 'Export As Image', 250);
    makeModalDialog('#help-dialog', 'Help', 300);

    // Give the tree menus an initial size limit.
    resizeTreeMenus();
}

// Make a dialog with specific modal behavior.
function makeModalDialog(id, title, width) {
    $(id).dialog({
        autoOpen: false,
        draggable: false,
        modal: true,
        position: { // Try not to cover up the toolbar/breadcrumb.
            my: "left top",
            at: "right top+" + (titleHeight + breadcrumbHeight),
            of: "#toolbar"
        },
        resizable: false,
        title: title,
        width: width
    });
}

// Request a tree build using the UI.
function dialogRequestTreeBuild() {
    treemapName = '';
    requestTreeBuild();
    closeBuildDialog();
}

// Display the treemap build dialog.
function openBuildDialog() {
    $('#build-dialog').dialog('open');
}

// Hide the treemap build dialog.
function closeBuildDialog() {
    $('#build-dialog').dialog('close');
}

// Display the treemap load dialog.
function openLoadDialog() {
    // Refetch the saves.
    initLoadTree();

    $('#load-dialog').dialog('open');
}

// Hide the treemap load dialog.
function closeLoadDialog() {
    $('#load-dialog').dialog('close');
}

// Display the treemap save dialog.
function openSaveDialog() {
    // Clear the textbox contents.
    $('#save-name').val('');

    // Clear the save error message if it exists.
    $('#save-error-message').text('');

    // Reset the save confirm boolean.
    confirmedSave = false;

    // Update the save button's state.
    updateSaveButton();

    $('#save-dialog').dialog('open');
}

// Hide the treemap save dialog.
function closeSaveDialog() {
    $('#save-dialog').dialog('close');
}

// Show the export dialog.
function openExportDialog() {
    $('#export-link').text('');
    
    $('#export-dialog').dialog('open');
}

// Show the help dialog.
function openHelpDialog() {
    $('#help-dialog').dialog('open');
}

// Creates the accordion behavior in the build dialog.
function initAccordion() {
    $('#accordion').accordion({
        collapsible: true,
        heightStyle: 'content'
    });
}
/* <-----------------------END---- JQUERY UI ------------------------------> */


/* <----------------------BEGIN--- BAD BROWSER WARNING ------------------------------> */
// Display an incompatibility warning and prevent using the site if canvas is unavailable.
function isBrowserUnsupported() {
    var canvasUnsupported = !window.HTMLCanvasElement;
    
    if (canvasUnsupported) {
        $('#browser-warning').show();
    }

    return canvasUnsupported;
}

// Handle the closing of the warning.
function removeBrowserWarning() {
    $('#browser-warning').hide();
}
/* <-----------------------END---- BAD BROWSER WARNING ------------------------------> */