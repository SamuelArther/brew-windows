class NowplayingCli < Formula
  desc "Retrieves currently playing media, and simulates media actions"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "nowplaying-cli", "nowplaying-cli"
  end

  test do
    assert_match "{", shell_output("#{bin}/nowplaying-cli get-raw")
    assert_equal "null", shell_output("#{bin}/nowplaying-cli get title").strip
  end
end
