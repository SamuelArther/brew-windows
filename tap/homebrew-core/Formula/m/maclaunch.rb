class Maclaunch < Formula
  desc "Manage Windows startup items"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "maclaunch", "maclaunch"
  end

  test do
    system bin/"maclaunch", "list"
    assert_match "startup items", shell_output("#{bin}/maclaunch --help")
    assert_match "no startup item named", shell_output("#{bin}/maclaunch disable no-such-item-brew-test 2>&1", 1)
  end
end
