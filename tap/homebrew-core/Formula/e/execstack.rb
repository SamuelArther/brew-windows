class Execstack < Formula
  desc "Utility to set/clear/query executable stack bit"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "execstack", "execstack"
  end

  test do
    cp test_fixtures("elf/hello"), testpath
    assert_match "- ", shell_output("#{bin}/execstack -q #{testpath}/hello")
    system bin/"execstack", "-s", testpath/"hello"
    assert_match "X ", shell_output("#{bin}/execstack -q #{testpath}/hello")
    system bin/"execstack", "-c", testpath/"hello"
    assert_match "- ", shell_output("#{bin}/execstack -q #{testpath}/hello")
    # Windows PE images: the flag is the inverse of NX_COMPAT (DEP)
    cp libexec/"wintools.exe", testpath/"pe.exe"
    assert_match "- ", shell_output("#{bin}/execstack -q #{testpath}/pe.exe")
  end
end
