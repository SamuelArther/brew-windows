class Rswift < Formula
  desc "Get strong typed, autocompleted resources like images, fonts and segues"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "rswift", "rswift"
  end

  test do
    assert_match(/^\d+\.\d+\.\d+$/, shell_output("#{bin}/rswift --version"))
    expected_output = "Error: Missing argument PROJECT_FILE_PATH"
    assert_match expected_output, shell_output("#{bin}/rswift generate #{testpath} 2>&1", 64)
    (testpath/"en.lproj").mkpath
    (testpath/"en.lproj/Localizable.strings").write "\"welcome.title\" = \"Hello\";\n"
    (testpath/"Assets.xcassets/AppLogo.imageset").mkpath
    system bin/"rswift", "generate", "--input-files", "en.lproj/Localizable.strings", "Assets.xcassets",
           "--", "R.generated.swift"
    generated = (testpath/"R.generated.swift").read
    assert_match "var welcomeTitle: RswiftResources.StringResource", generated
    assert_match "developmentValue: \"Hello\"", generated
    assert_match "var appLogo: RswiftResources.ImageResource", generated
  end
end
