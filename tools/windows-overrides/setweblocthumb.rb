class Setweblocthumb < Formula
  desc "Assign page thumbnail icons to .webloc and .url web links"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "setWeblocThumb", "setWeblocThumb"
  end

  test do
    ENV["LOCALAPPDATA"] = testpath.to_s
    Pathname.new("google.webloc").write('{URL = "https://google.com";}')
    # -t draws the icon (a tile with the site's initial) without loading the page.
    assert_match "thumbnail icon set on google.url", shell_output("#{bin}/setWeblocThumb -t google.webloc")
    shortcut = (testpath/"google.url").read
    assert_match "URL=https://google.com", shortcut
    icon = shortcut[/^IconFile=(.+)$/, 1].strip
    assert_equal [0, 0, 1, 0], File.binread(icon).bytes.first(4)
    assert_match "Usage: setWeblocThumb", shell_output("#{bin}/setWeblocThumb --help")
  end
end
