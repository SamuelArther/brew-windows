class Jq < Formula
  desc "Lightweight and flexible command-line JSON processor"
  homepage "https://jqlang.org/"
  url "https://github.com/jqlang/jq/releases/download/jq-1.8.2/jq-windows-amd64.exe"
  version "1.8.2"
  sha256 "a6fc67fedaf9128a3309a1e2ebb8b986aeccf70122ee46d2cb4849e423f0c627"
  license "MIT"

  livecheck do
    url :stable
    regex(/^(?:jq[._-])?v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "jq-windows-amd64.exe" => "jq.exe"
  end

  test do
    assert_equal "2\n", pipe_output("#{bin}/jq .bar", '{"foo":1, "bar":2}')
  end
end
