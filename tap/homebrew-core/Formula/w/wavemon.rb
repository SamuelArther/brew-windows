class Wavemon < Formula
  desc "Monitor Wi-Fi link quality and nearby access points"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "wavemon", "wavemon"
  end

  test do
    assert_match "wavemon: option requires an argument -- 'i'", shell_output("#{bin}/wavemon -i 2>&1", 2)
    assert_match "wavemon", shell_output("#{bin}/wavemon -v")
    assert_match "no supported wireless interfaces", shell_output("#{bin}/wavemon 2>&1", 1)
  end
end
