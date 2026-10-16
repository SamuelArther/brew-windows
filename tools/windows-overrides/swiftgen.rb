class Swiftgen < Formula
  desc "Swift code generator for assets, storyboards, Localizable.strings, etc."
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "swiftgen", "swiftgen"
  end

  test do
    system bin/"swiftgen", "--version"
    (testpath/"colors.txt").write "ArticleBody: #339666\nCyan: #ff66ccff\n"
    output = shell_output("#{bin}/swiftgen run colors --templateName swift5 colors.txt")
    assert_match "internal static let articleBody = ColorName(rgbaValue: 0x339666ff)", output
    (testpath/"en.lproj").mkpath
    (testpath/"en.lproj/Localizable.strings").write "\"apples.count\" = \"You have %d apples\";\n"
    (testpath/"Assets.xcassets/Logo.imageset").mkpath
    (testpath/"swiftgen.yml").write <<~YAML
      strings:
        inputs: en.lproj
        outputs:
          - templateName: structured-swift5
            output: Generated/Strings.swift
      xcassets:
        inputs: Assets.xcassets
        outputs:
          templateName: swift5
          output: Generated/Assets.swift
    YAML
    system bin/"swiftgen", "config", "run"
    assert_match "internal static func count(_ p1: Int) -> String", (testpath/"Generated/Strings.swift").read
    assert_match "internal static let logo = ImageAsset(name: \"Logo\")", (testpath/"Generated/Assets.swift").read
  end
end
