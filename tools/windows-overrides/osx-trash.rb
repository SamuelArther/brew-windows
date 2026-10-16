class OsxTrash < Formula
  desc "Move files to the Recycle Bin instead of tempting fate with rm"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

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
