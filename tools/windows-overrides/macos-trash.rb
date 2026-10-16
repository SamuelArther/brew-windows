class MacosTrash < Formula
  desc "Move files and folders to the Recycle Bin"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "trash", "trash"
  end

  test do
    (testpath/"junk.txt").write "x"
    system bin/"trash", testpath/"junk.txt"
    refute_path_exists testpath/"junk.txt"
    assert_match "Recycle Bin", shell_output("#{bin}/trash --help")
  end
end
