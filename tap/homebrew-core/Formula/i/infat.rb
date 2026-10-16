class Infat < Formula
  desc "Tool to set default openers for file formats and url schemes"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "infat", "infat"
  end

  test do
    # set would open Settings to confirm, so only read
    assert_match(/Default for \.txt: .*notepad/i, shell_output("#{bin}/infat info --ext txt"))
    assert_match(/infat \d+\.\d+/, shell_output("#{bin}/infat --version"))
  end
end
