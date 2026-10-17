class Httpx < Formula
  desc "Fast and multi-purpose HTTP toolkit"
  homepage "https://github.com/projectdiscovery/httpx"
  url "https://github.com/projectdiscovery/httpx/archive/refs/tags/v1.12.0.tar.gz"
  sha256 "b29319c085537047bff8acf72c6cd4ecf17585ede97f4afdde2304431c74e64e"
  license "MIT"
  head "https://github.com/projectdiscovery/httpx.git", branch: "dev"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7876b60085dc14e4a39085bd3592fe02b9e775f36ebb6603d19f8471e1307caa"
  end

  depends_on "go" => :build

  # `test do` block probes example.org
  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/httpx"
  end

  test do
    output = JSON.parse(shell_output("#{bin}/httpx -silent -title -json -u example.org"))
    assert_equal 200, output["status_code"]
    assert_equal "Example Domain", output["title"]
  end
end
