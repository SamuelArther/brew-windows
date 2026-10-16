class Execstack < Formula
  desc "Utility to set/clear/query executable stack bit"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
