class Afsctool < Formula
  desc "Utility for manipulating NTFS and Windows Overlay Filter compressed files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "afsctool", "afsctool"
  end

  test do
    path = testpath/"foo"
    sample = "A" * 1024 * 1024
    path.write sample

    [[], ["-T", "LZX"], ["-T", "LZNT1"]].each do |x|
      system bin/"afsctool", "-c", *x, path
      assert_match "File is compressed", shell_output("#{bin}/afsctool -v #{path}")
      system bin/"afsctool", "-d", path
      assert_match "File is not compressed", shell_output("#{bin}/afsctool #{path}")
      assert_equal sample, path.read
    end
  end
end
