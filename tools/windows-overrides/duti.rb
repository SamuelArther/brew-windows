class Duti < Formula
  desc "Select default apps for documents and URL schemes"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
