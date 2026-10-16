class CreateDmg < Formula
  desc "Build mountable ISO/UDF disk images from a folder, with create-dmg's options"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "create-dmg", "create-dmg"
  end

  test do
    File.write(testpath/"Brew-Eula.txt", "Eula")
    (testpath/"Test-Source").mkpath
    (testpath/"Test-Source/Brew.app").mkpath
    (testpath/"Test-Source/Brew.app/readme.txt").write "hello"
    system bin/"create-dmg", "--sandbox-safe", "--eula",
           testpath/"Brew-Eula.txt", testpath/"Brew-Test.dmg", testpath/"Test-Source"
    image = File.binread(testpath/"Brew-Test.dmg")
    assert_equal "CD001", image[0x8001, 5]
    assert_match "create-dmg", shell_output("#{bin}/create-dmg --version")
  end
end
