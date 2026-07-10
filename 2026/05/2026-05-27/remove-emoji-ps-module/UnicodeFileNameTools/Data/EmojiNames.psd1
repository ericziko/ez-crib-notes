#
# Curated emoji -> shortcode table for UnicodeFileNameTools.
#
# Keys are the Unicode scalar value in uppercase hexadecimal with no padding and
# no "U+" prefix, matching ('{0:X}' -f <codepoint>). Values are filesystem-safe
# kebab-case shortcodes (the substitution pipeline wraps them as :shortcode:).
#
# This table is intentionally NON-EXHAUSTIVE: it covers commonly encountered
# emoji. Callers can extend or override it for any code point via the -Map
# parameter of Convert-UnicodeFileName / Repair-UnicodeFileName, which takes
# priority over this table.
#
@{
    # --- Faces & emotion ---
    '1F600' = 'grinning-face'
    '1F601' = 'beaming-face'
    '1F602' = 'face-with-tears-of-joy'
    '1F603' = 'grinning-face-with-big-eyes'
    '1F604' = 'grinning-face-with-smiling-eyes'
    '1F609' = 'winking-face'
    '1F60A' = 'smiling-face-with-smiling-eyes'
    '1F60D' = 'smiling-face-with-heart-eyes'
    '1F618' = 'face-blowing-a-kiss'
    '1F61C' = 'winking-face-with-tongue'
    '1F620' = 'angry-face'
    '1F622' = 'crying-face'
    '1F62D' = 'loudly-crying-face'
    '1F642' = 'slightly-smiling-face'
    '1F644' = 'face-with-rolling-eyes'
    '1F910' = 'zipper-mouth-face'
    '1F914' = 'thinking-face'
    '1F923' = 'rolling-on-the-floor-laughing'
    '1F929' = 'star-struck'
    '1F970' = 'smiling-face-with-hearts'

    # --- Hands & body ---
    '1F44B' = 'waving-hand'
    '1F44D' = 'thumbs-up'
    '1F44E' = 'thumbs-down'
    '1F44F' = 'clapping-hands'
    '1F4AA' = 'flexed-biceps'
    '1F64C' = 'raising-hands'
    '1F64F' = 'folded-hands'
    '1F918' = 'sign-of-the-horns'

    # --- Hearts & feelings ---
    '2764'  = 'red-heart'
    '1F494' = 'broken-heart'
    '1F496' = 'sparkling-heart'
    '1F499' = 'blue-heart'
    '1F49A' = 'green-heart'
    '1F49B' = 'yellow-heart'
    '1F49C' = 'purple-heart'

    # --- Stars, marks & alerts ---
    '2728'  = 'sparkles'
    '2B50'  = 'star'
    '1F31F' = 'glowing-star'
    '2705'  = 'check-mark-button'
    '274C'  = 'cross-mark'
    '2757'  = 'exclamation-mark'
    '2753'  = 'question-mark'
    '26A0'  = 'warning'
    '1F4AF' = 'hundred-points'
    '1F525' = 'fire'

    # --- Celebration & objects ---
    '1F389' = 'party-popper'
    '1F38A' = 'confetti-ball'
    '1F388' = 'balloon'
    '1F381' = 'wrapped-gift'
    '1F382' = 'birthday-cake'
    '1F680' = 'rocket'
    '1F4A1' = 'light-bulb'
    '1F4A9' = 'pile-of-poo'
    '1F4DD' = 'memo'
    '1F4C1' = 'file-folder'
    '1F4C4' = 'page-facing-up'
    '1F4CC' = 'pushpin'
    '1F511' = 'key'
    '1F512' = 'locked'
    '1F513' = 'unlocked'
    '1F517' = 'link'
    '1F4E7' = 'e-mail'
    '1F4F1' = 'mobile-phone'
    '1F4BB' = 'laptop'
    '1F516' = 'bookmark'
    '1F50D' = 'magnifying-glass-tilted-left'
    '1F4CA' = 'bar-chart'
    '1F4C8' = 'chart-increasing'
    '1F4C9' = 'chart-decreasing'

    # --- Nature & animals ---
    '1F436' = 'dog-face'
    '1F431' = 'cat-face'
    '1F984' = 'unicorn'
    '1F981' = 'lion'
    '1F438' = 'frog'
    '1F40D' = 'snake'
    '1F41B' = 'bug'
    '1F33F' = 'herb'
    '1F340' = 'four-leaf-clover'
    '1F308' = 'rainbow'
    '2600'  = 'sun'
    '2601'  = 'cloud'
    '2614'  = 'umbrella-with-rain-drops'
    '26C4'  = 'snowman'
    '26A1'  = 'high-voltage'

    # --- Food & drink ---
    '1F355' = 'pizza'
    '1F354' = 'hamburger'
    '1F35F' = 'french-fries'
    '2615'  = 'hot-beverage'
    '1F37A' = 'beer-mug'
    '1F377' = 'wine-glass'
    '1F36B' = 'chocolate-bar'

    # --- Travel, money & time ---
    '1F697' = 'automobile'
    '2708'  = 'airplane'
    '1F30D' = 'globe-europe-africa'
    '1F4B0' = 'money-bag'
    '1F4B8' = 'money-with-wings'
    '23F0'  = 'alarm-clock'
    '231A'  = 'watch'

    # --- Arrows (emoji presentation) ---
    '27A1'  = 'right-arrow'
    '2B05'  = 'left-arrow'
    '2B06'  = 'up-arrow'
    '2B07'  = 'down-arrow'
}
