# Migrating to v2

SB Admin Pro v2 uses Bootstrap 5, which comes with a significant amount of
breaking changes.

To start, review the Bootstrap 5 migration guide on Bootstrap's official
documentation here: <https://getbootstrap.com/docs/5.0/migration/>.

Below we have outlined the specific changes you should make you your markup
in order to migrate versions.

## Dependencies

- Dropped jQuery
- Upgraded to Bootstrap 5

To update dependencies, navigate to the `src/pug/includes/scripts.pug` file and:

1. Delete jQuery
2. Replace the Bootstrap 4 JS bundle with the Bootstrap 5 JS bundle, which you can find on Bootstrap's website
at <https://getbootstrap.com/docs/5.0/getting-started/introduction/#bundle>.

### DataTables

The dataTables plugin was dropped in favor of Simple DataTables which is a JS only solution to sortable,
searchable data tables.

Usage for these data tables can be seen on the dashboard demos and the tables demo page. Go to
`src/js/datatables/` to see the initialization scripts and the `src/pug/pages/includes/` to see the markup
for the data tables. The third party resources are brought in via CDN on pages that use tables, for example, the
`tables.pug` page. Visit <https://github.com/fiduswriter/Simple-DataTables> for more options and full documentation
of the plugin.

### Date Range Picker

The Date Range Picker plugin was dropped in favor of Litepicker which is a JS only solution to add date and
date range picker functionality to form inputs. Visit `src/js/litepicker.js` to see the initialization scripts
and the `forms.pug` page for usage examples.

The third party resources are brought in via CDN on pages that use the plugin, and you can visit
<https://litepicker.com/> for more options and full documentation of the plugin.

## Data Attributes

Data attributes for all JavaScript plugins are now namespaced to help distinguish Bootstrap
functionality from third parties and your own code. For example, Bootstrap now uses `data-bs-toggle` instead of
`data-toggle`.

In SB Admin Pro, make the following changes:

- Replace `data-toggle` with `data-bs-toggle`.
- Replace `data-target` with `data-bs-target`.
- Replace `data-dismiss` with `data-bs-dismiss`.
- Replace `data-parent` with `data-bs-parent`.

Make sure to also replace any other Bootstrap plugin data attributes where used.

## Grid

For SB Admin Pro, we have reworked the spacing of the grid. For all containers, do the following:

- Replace all instances of the `.container` class with `.container-xl` and add the `.px-4` utility class
to the container for consistent spacing.
- Add the `.px-4` utility class to all instances of `.container-fluid` as well.

Row gutters have also been modified throughout the theme to increase spacing past the Bootstrap defaults. For
all rows, do the following:

- Add the `.gx-4` class to all instances of `.row` elements.
- Replace the `.no-gutters` class with `.gx-0`.

## Components

### Accordion

- The `.accordion-faq` component has been deprecated. Use the new accordion markup from the Bootstrap 5
component instead.

### Badges

- Badges no longer use contextual classes for coloring, instead they use background color utilities. For example,
change `.badge .badge-primary` to `.badge .bg-primary .text-white`. Note that a text color utilitiy is needed
for badges with darker background colors.
- Badges no longer use the `.badge-pill` distinction and should be replaced with the `.rounded-pill` utility.
- Change custom badge utilities `.badge-transparent-*` to `.bg-transparent-*`.

### Buttons

- Any `&lt;button&gt;` element with the `.close` class applied should be changed to `.btn-close`, for example,
within dismissible alerts. The hidden 'x' which is a child of the button element should be deleted as well.
- The `.btn-block` class has been deprecaated. Wrap buttons in a `.d-grid` utility to create block buttons.

### Cards

- The masonry layout for cards has been deprecated in Bootstrap 5. Use rows or flex utilities to handle
masonry style layouts, or use an external plugin.

### Dropdowns

- Replace all `.dropdown-menu-right` classes with `.dropdown-menu-end`

### Media

- The Bootstrap 4 media object has been deprecated. Use flexbox utilities for the `.media` classes instead
of the old media object.

### Topnav

- The `.topnav` was changed to include the toggle button before the navbar brand, use the new topnav layout from
the Pug include or from any of the demo pages if using HTML

## Forms

- Custom checkbox and radio components were rewritten and now use the `.form-check` class instead of the former
custom class names. For more information on this change, visit <https://getbootstrap.com/docs/5.0/forms/checks-radios/>
- Input groups no longer use the `.input-group-prepend` and `.input-group-append` classes. Remove these and use
 the child `.input-group-text` in their place. See <https://getbootstrap.com/docs/5.0/forms/input-group/> for more
 details.
- Padding utilities `.ps-0` and `.pe-0` are used on the custom joined input groups component to adjust the padding
of the inputs based on their location before or after the input group text.
- The `.form-row` and `.form-group` elements have been deprecated. Use margin utilities and Bootstrap rows and
columns instead. For examples, see the form component page.

## Utilities

For class name changes, we recommend using the 'find and replace all' function
within a code editor. Overall, any mention of `left` and `right` in the code has
been renamed to use `start` and `end` to allow for RTL support within Bootstrap 5.

Below are the specific changes that apply to the SB Admin Pro theme. Following these
steps should bring you completely up to date for SB Admin Pro v2.

### Borders

- Replace the `.rounded-left` and `.rounded-right` utilities with `.rounded-start` and `.rounded-end`, respectively.
- Replace the `.border-left` and `.border-right` utilities with `.border-start` and `.border-end`, respectively.
- Replace the `.border-lg-left` and `.border-lg-right` utilities with `.border-lg-start` and `.border-lg-end`, respectively.
- Replace rounded border sizes `.rounded-lg` and `.rounded-sm` with the new numeric rounding system. For example,
change `.rounded-lg` to `.rounded-3`. Rounded utilities 0-3 are available with Bootstrap 5.

### Spacing

- Replace all `.ml-*` utilities with `.ms-*`, for example `.ml-4` should change to `ms-4`.
- Replace all `.mr-*` utilities with `.me-*`, for example `.mr-4` should change to `me-4`.
- Replace all `.pl-*` utilities with `.ps-*`, for example `.pl-4` should change to `ps-4`.
- Replace all `.pr-*` utilities with `.pe-*`, for example `.pr-4` should change to `pe-4`.

### Text

- Replace all `.font-weight-*` utilities with `fw-*`, for example `.font-weight-bold` should change to `.fw-bold`.
- Replace `.text-left` and `text-right` utilties with `.text-start` and `.text-end`, respectively. Be aware that
responsive variants of these classes, for example `.text-lg-left` need to be renamed as well, for example, to
`.text-lg-start`.

### Float

- The `.float-*` utility uses RTL friendly naming. Use `.float-end` and `.float-start` instead of `.float-right` and
`.float-left`, respectively.
