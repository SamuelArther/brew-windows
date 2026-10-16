class Duti < Formula
  desc "Select default apps for documents and URL schemes"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
