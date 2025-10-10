# Visual Identity Color Scheme Guide

## Primary Brand Colors

### UN Blue 🔵
**Primary brand color - symbolizes peace and the boundless sky**
- **RGB:** R0 G158 B219
- **Hex:** `#009EDB`
- **CMYK:** C80 M20 Y0 K0
- **Pantone:** 2925
- **Usage:** Main brand color for headers, primary buttons, links

#### UN Blue Variations
- **Tint Light:** `#C5DFEF` (RGB: 197, 223, 239) - Pantone 290
- **Tint Extra Light:** `#E3EDF6` (RGB: 227, 237, 246) - 50% Pantone 290
- **Shade Dark:** `#004987` (RGB: 0, 73, 135) - Pantone 301
- **Accessible AA:** `#0077B8`
- **Accessible AAA:** `#005392`

### Black ⚫
**For text and high contrast elements**
- **RGB:** R0 G0 B0
- **Hex:** `#000000`
- **CMYK:** C0 M0 Y0 K100
- **Usage:** Primary text, headings

### White ⚪
**For backgrounds and reverse text**
- **RGB:** R255 G255 B255
- **Hex:** `#FFFFFF`
- **CMYK:** C0 M0 Y0 K0
- **Usage:** Page backgrounds, text on dark backgrounds

---

## Accent Colors

### Green 🟢
**Represents thriving flora and growth**
- **Primary:** `#72BF44` (RGB: 114, 191, 68) - Pantone 368
- **Light Tint:** `#CEE3A0` (RGB: 206, 227, 160) - Pantone 365
- **Extra Light:** `#E6EFD0` (RGB: 230, 239, 208) - 50% Pantone 365
- **Dark Shade:** `#006747` (RGB: 0, 103, 71) - Pantone 7728
- **Accessible AA:** `#27833A`
- **Usage:** Success states, approved status, positive actions

### Yellow 🟡
**Represents optimism and new beginnings**
- **Primary:** `#FFC800` (RGB: 255, 200, 0) - Pantone 7548
- **Light Tint:** `#F8E66B` (RGB: 248, 230, 107) - Pantone 106
- **Extra Light:** `#FAF0BB` (RGB: 250, 240, 187) - 50% Pantone 106
- **Dark Shade:** `#B16D03` (RGB: 177, 109, 3) - Pantone 139
- **Usage:** Warnings, pending states, highlights

### Orange 🟠
**Represents energy and nourishment**
- **Primary:** `#F58220` (RGB: 245, 130, 32) - Pantone 715
- **Light Tint:** `#FEDCBD` (RGB: 254, 220, 189) - Pantone 475
- **Extra Light:** `#FFEAD5` (RGB: 255, 234, 213) - 50% Pantone 475
- **Dark Shade:** `#D4582A` (RGB: 212, 88, 42) - Pantone 7579
- **Accessible AA:** `#CF3F0B`
- **Usage:** Important notices, in-progress states

### Red 🔴
**Represents urgency and collective action**
- **Primary:** `#ED1847` (RGB: 237, 24, 71) - Pantone 192
- **Light Tint:** `#F9C0C5` (RGB: 249, 192, 197) - Pantone 176
- **Extra Light:** `#F7DFDF` (RGB: 247, 223, 223) - 50% Pantone 176
- **Dark Shade:** `#A71F36` (RGB: 167, 31, 54) - Pantone 201
- **Accessible AA:** `#EB0045`
- **Accessible AAA:** `#AB1D37`
- **Usage:** Errors, rejections, critical alerts, delete actions

### Purple 🟣
**Represents possibility and innovation**
- **Primary:** `#A05FB4` (RGB: 160, 95, 180) - Pantone 2583
- **Light Tint:** `#D5B4D6` (RGB: 213, 180, 214) - Pantone 2563
- **Extra Light:** `#E4D7E8` (RGB: 228, 215, 232) - 50% Pantone 2563
- **Dark Shade:** `#5B2C86` (RGB: 91, 44, 134) - Pantone 2597
- **Accessible AA:** `#9A58AF`
- **Accessible AAA:** `#733D96`
- **Usage:** Special features, premium content

### Gray 🔘
**Represents stability and neutrality**
- **Primary:** `#AEA29A` (RGB: 170, 160, 150) - Pantone Warm Gray 5
- **Light Tint:** `#DED9D5` (RGB: 222, 217, 213) - Pantone Warm Gray 1
- **Extra Light:** `#EBEAE6` (RGB: 235, 234, 230) - 50% Pantone Warm Gray 1
- **Dark Shade:** `#6E6259` (RGB: 110, 98, 89) - Pantone Warm Gray 11
- **Accessible AA:** `#7C7067`
- **Usage:** Disabled states, borders, secondary text

---

## Implementation in CSS

### CSS Variables
```css
:root {
  /* Primary Colors */
  --un-blue: #009EDB;
  --un-blue-light: #C5DFEF;
  --un-blue-extra-light: #E3EDF6;
  --un-blue-dark: #004987;
  --un-blue-accessible: #0077B8;
  
  --un-black: #000000;
  --un-white: #FFFFFF;
  
  /* Accent Colors */
  --un-green: #72BF44;
  --un-green-light: #CEE3A0;
  --un-green-dark: #006747;
  
  --un-yellow: #FFC800;
  --un-yellow-light: #F8E66B;
  --un-yellow-dark: #B16D03;
  
  --un-orange: #F58220;
  --un-orange-light: #FEDCBD;
  --un-orange-dark: #D4582A;
  
  --un-red: #ED1847;
  --un-red-light: #F9C0C5;
  --un-red-dark: #A71F36;
  
  --un-purple: #A05FB4;
  --un-purple-light: #D5B4D6;
  --un-purple-dark: #5B2C86;
  
  --un-gray: #AEA29A;
  --un-gray-light: #DED9D5;
  --un-gray-extra-light: #EBEAE6;
  --un-gray-dark: #6E6259;
}
```

---

## Usage Guidelines

### Primary Usage Rules
1. **UN Blue, white, and black** should be the most prominent colors
2. Use UN Blue universally to maintain brand clarity and dignity
3. White backgrounds with black text for maximum readability

### Accent Color Usage
- Use **sparingly** across content
- Primarily for differentiating large amounts of information
- Ideal for charts, graphs, maps, and data visualization
- Do NOT color-code accent colors to represent specific entities

### Status Color Mapping
| Status | Color | Hex Code |
|--------|-------|----------|
| Success/Approved | Green | `#72BF44` |
| Warning/Pending | Yellow/Orange | `#FFC800` / `#F58220` |
| Error/Rejected | Red | `#ED1847` |
| Info/Active | UN Blue | `#009EDB` |
| Disabled/Inactive | Gray | `#AEA29A` |

### Accessibility Guidelines
- Ensure suitable contrast ratios (WCAG AA/AAA compliance)
- Don't place accent colors on similar brightness backgrounds
- Use accessible color variants when needed:
  - AA compliant: For normal text (4.5:1 contrast ratio)
  - AAA compliant: For enhanced accessibility (7:1 contrast ratio)

### Special Notice
⚠️ **SDG Colors:** Use SDG colors ONLY within SDG contexts. The accent color palette is separate from the SDG color wheel and should not be tied to topical meaning.

---

## Component Examples

### Buttons
```css
.btn-primary { background: #009EDB; } /* UN Blue */
.btn-success { background: #72BF44; } /* Green */
.btn-warning { background: #FFC800; } /* Yellow */
.btn-danger { background: #ED1847; } /* Red */
.btn-secondary { background: #AEA29A; } /* Gray */
```

### Status Badges
```css
.badge-approved { background: #72BF44; color: #FFFFFF; }
.badge-pending { background: #F58220; color: #FFFFFF; }
.badge-rejected { background: #ED1847; color: #FFFFFF; }
.badge-draft { background: #AEA29A; color: #FFFFFF; }
```

### Backgrounds
```css
.bg-light { background: #E3EDF6; } /* UN Blue Extra Light */
.bg-section { background: #EBEAE6; } /* Gray Extra Light */
.bg-highlight { background: #FAF0BB; } /* Yellow Extra Light */
```

---

## Military/Peacekeeping
**Special Application**
- Pantone: 16-4134 TPG
- Used for: Peacekeepers' berets, helmets, scarves, shoulder patches
- This is the same as UN Blue but specified for textile/military equipment

---

*This color scheme ensures consistency with UN Visual Identity guidelines while maintaining accessibility and usability standards for digital applications.*