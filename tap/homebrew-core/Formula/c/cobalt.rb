class Cobalt < Formula
  desc "Static site generator written in Rust"
  homepage "https://cobalt-org.github.io/"
  url "https://github.com/cobalt-org/cobalt.rs/releases/download/v0.20.4/cobalt-v0.20.4-x86_64-pc-windows-msvc.zip"
  sha256 "19c4ed116c4336d4c3bf0826ac11edb66a43713445d747a9357e78e9b1dec5e8"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cobalt-org/cobalt.rs/archive/refs/tags/v0.20.4.tar.gz"
  end

  def install
    bin.install "cobalt.exe"
  end

  test do
    system bin/"cobalt", "init"
    system bin/"cobalt", "build"
    assert_path_exists testpath/"_site/index.html"
  end
end
