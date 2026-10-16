class Clipper < Formula
  desc "Share the Windows clipboard with tmux and other local and remote apps"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "clipper", "clipper"
  end

  test do
    assert_match "clipper", shell_output("#{bin}/clipper -v")
    assert_match "--port", shell_output("#{bin}/clipper --help")
    assert_match "flag provided but not defined", shell_output("#{bin}/clipper --bogus 2>&1", 2)
  end
end
