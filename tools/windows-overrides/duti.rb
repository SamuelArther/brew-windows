class Duti < Formula
  desc "Select default apps for documents and URL schemes"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "duti", "duti"
  end

  test do
    assert_match "no handlers", shell_output("#{bin}/duti -l public.nonexistent-type 2>&1", 1)
    assert_match(/notepad/i, shell_output("#{bin}/duti -x txt"))
  end
end
