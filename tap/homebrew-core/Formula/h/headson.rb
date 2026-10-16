class Headson < Formula
  desc "Head/tail for structured data"
  homepage "https://docs.rs/headson/latest/headson/"
  url "https://github.com/kantord/headson/releases/download/headson-v0.17.1/hson-x86_64-pc-windows-msvc.tar.gz"
  version "0.17.1"
  sha256 "1e1d2f7d840e6f3b2d111333b9457b512f9cb6df9e941f1a39832d88fe09e7d6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kantord/headson/archive/refs/tags/headson-v0.17.1.tar.gz"
  end

  def install
    bin.install "hson.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hson --version")

    (testpath/"test.json").write '{"a":1,"b":[2,3]}'
    assert_match '"a":1', shell_output("#{bin}/hson --compact #{testpath}/test.json")
  end
end
