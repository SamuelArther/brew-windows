class Dum < Formula
  desc "Npm scripts runner written in Rust"
  homepage "https://github.com/egoist/dum"
  url "https://github.com/egoist/dum/releases/download/v0.1.20/dum-x86_64-pc-windows-msvc.zip"
  sha256 "3759d385ca4559ac62f63542aa6955a16c9fd9b8978e7854925a7bea5d5bf32c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/egoist/dum/archive/refs/tags/v0.1.20.tar.gz"
  end

  def install
    bin.install "dum.exe"
  end

  test do
    (testpath/"package.json").write <<~JSON
      {
        "scripts": {
          "hello": "echo 'Hello, dum!'"
        }
      }
    JSON

    output = shell_output("#{bin}/dum run hello")
    assert_match "Hello, dum!", output

    assert_match version.to_s, shell_output("#{bin}/dum --version")
  end
end
