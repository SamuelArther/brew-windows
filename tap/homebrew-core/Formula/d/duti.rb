class Duti < Formula
  desc "Select default apps for documents and URL schemes"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
