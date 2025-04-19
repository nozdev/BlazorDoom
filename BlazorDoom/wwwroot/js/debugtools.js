class DebugTools {
  /**
 *
 * @param {number[]} colors unit array
 * @returns {number[]} the new palette
 */
  static showPalette(colors) {
    const paletteChoice = document.getElementById('palette-choice');
    switch (paletteChoice.value) {
      case "default":
        break;
      case "grayscale":
        colors = colors.map(color => {
          const r = color & 0xff;
          const g = (color >> 8) & 0xff;
          const b = (color >> 16) & 0xff;
          const gray = (r + g + b) / 3;
          return (gray << 16) | (gray << 8) | gray;
        });
        break;
      case "8colors":
        colors = colors.map(color => {
          let r = color & 0xff;
          let g = (color >> 8) & 0xff;
          let b = (color >> 16) & 0xff;
          r = r > 128 ? 255 : 0;
          g = g > 128 ? 255 : 0;
          b = b > 128 ? 255 : 0;
          return (r << 16) | (g << 8) | b;
        });
        break;
      case "blueman":
        colors = colors.map(color => {
          let r = color & 0xff;
          let g = (color >> 8) & 0xff;
          let b = (color >> 16) & 0xff;
          r = Math.floor(r / 8) * 8;
          g = Math.floor(g / 8) * 8;
          b = Math.floor(b / 8) * 8;
          return (r << 16) | (g << 8) | b;
        });
        break;
    }

    const paletteContainer = document.getElementById('color-palette');
    paletteContainer.innerHTML = '';
    for (let i = 0; i < colors.length; i++) {
      const colorEl = document.createElement('div');
      const color = colors[i];
      const r = color & 0xff;
      const g = (color >> 8) & 0xff;
      const b = (color >> 16) & 0xff;
      colorEl.style.backgroundColor = `rgb(${r}, ${g}, ${b})`;
      colorEl.style.width = '20px';
      colorEl.style.height = '20px';
      colorEl.style.display = 'inline-block';
      paletteContainer.appendChild(colorEl);
    }
    return colors;
  }
}