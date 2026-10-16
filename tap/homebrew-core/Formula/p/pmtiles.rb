class Pmtiles < Formula
  desc "Single-file executable tool for creating, reading and uploading PMTiles archives"
  homepage "https://protomaps.com/docs/pmtiles"
  url "https://github.com/protomaps/go-pmtiles/archive/refs/tags/v1.31.2.tar.gz"
  sha256 "022386795fae441dd35927e4061daaed8cda7e21ba0a2e2253c8fd0d85bc482f"
  license "BSD-3-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1d9912c0b9d7655705163ac6a261970b365b7b3ab2f4ca58a898c4cede5d50ca"
  end

  depends_on "go" => :build

  # `test do` block runs a local tile server
  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    port = free_port
    pid = spawn bin/"pmtiles", "serve", ".", "--port", port.to_s
    sleep 3
    output = shell_output("curl -sI http://localhost:#{port}")
    assert_match "HTTP/1.1 204 No Content", output
  ensure
    Process.kill("HUP", pid)
  end
end
