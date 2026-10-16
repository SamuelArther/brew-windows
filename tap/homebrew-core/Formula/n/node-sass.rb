class NodeSass < Formula
  desc "JavaScript implementation of a Sass compiler"
  homepage "https://github.com/sass/dart-sass"
  url "https://github.com/sass/dart-sass/releases/download/1.105.1/dart-sass-1.105.1-windows-x64.zip"
  sha256 "3f76ae65dd7b494cc25cd2257f3be608e074b2d33b7328d9ba565bd60ba54f7e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://registry.npmjs.org/sass/-/sass-1.105.1.tgz"
  end

  def install
    install_tree
    windows_cmd "libexec/sass.bat", "sass"
  end

  test do
    (testpath/"test.scss").write <<~SCSS
      div {
        img {
          border: 0px;
        }
      }
    SCSS

    assert_equal "div img{border:0px}",
    shell_output("#{bin}/sass --style=compressed test.scss").strip
  end
end
