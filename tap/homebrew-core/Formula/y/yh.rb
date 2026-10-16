class Yh < Formula
  desc "YAML syntax highlighter to bring colours where only jq could"
  homepage "https://github.com/andreazorzetto/yh"
  url "https://github.com/andreazorzetto/yh/releases/download/v0.4.0/yh-windows-amd64.zip"
  sha256 "c2c78f3bec6dc55c1485c83e74831812002a07e18640c598ff31a817ecfc7e3d"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/andreazorzetto/yh/archive/refs/tags/v0.4.0.tar.gz"
  end

  def install
    bin.install "yh.exe"
  end

  test do
    assert_equal "\e[91mfoo\e[0m: \e[33mbar\e[0m\n", pipe_output(bin/"yh", "foo: bar")
  end
end
