class DartSass < Formula
  desc "Reference implementation of Sass, written in Dart"
  homepage "https://sass-lang.com/dart-sass"
  url "https://github.com/sass/dart-sass/releases/download/1.105.1/dart-sass-1.105.1-windows-x64.zip"
  sha256 "3f76ae65dd7b494cc25cd2257f3be608e074b2d33b7328d9ba565bd60ba54f7e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sass/dart-sass/archive/refs/tags/1.105.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_cmd "libexec/sass.bat", "sass"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sass --version")

    (testpath/"test.scss").write(".class {property: 1 + 1}")
    assert_match "property: 2;", shell_output("#{bin}/sass test.scss 2>&1")

    (testpath/"input.scss").write <<~SCSS
      div {
        img {
          border: 0px;
        }
      }
    SCSS

    assert_equal "div img{border:0px}",
    shell_output("#{bin}/sass --style compressed input.scss").strip
  end
end
