class Wallpaper < Formula
  desc "Manage the desktop wallpaper"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "wallpaper", "wallpaper"
  end

  test do
    system bin/"wallpaper", "get"
    assert_match "set-solid-color", shell_output("#{bin}/wallpaper --help")
    assert_match "invalid colour", shell_output("#{bin}/wallpaper set-solid-color nothex 2>&1", 2)
  end
end
