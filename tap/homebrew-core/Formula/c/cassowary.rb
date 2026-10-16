class Cassowary < Formula
  desc "Modern cross-platform HTTP load-testing tool written in Go"
  homepage "https://github.com/rogerwelin/cassowary"
  url "https://github.com/rogerwelin/cassowary/releases/download/v0.19.0/cassowary_Windows_x86_64.tar.gz"
  sha256 "118ce141583158fa4591de273e22e6f55dc5bd7417b03111b44acf3a12e15236"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rogerwelin/cassowary/archive/refs/tags/v0.19.0.tar.gz"
  end

  def install
    bin.install "cassowary.exe"
  end

  test do
    system(bin/"cassowary", "run", "-u", "http://www.example.com", "-c", "10", "-n", "100", "--json-metrics")
    assert_match "\"base_url\":\"http://www.example.com\"", File.read("#{testpath}/out.json")

    assert_match version.to_s, shell_output("#{bin}/cassowary --version")
  end
end
