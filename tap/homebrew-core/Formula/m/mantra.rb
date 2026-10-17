class Mantra < Formula
  desc "Tool to hunt down API key leaks in JS files and pages"
  homepage "https://amolo.com.br"
  url "https://github.com/brosck/mantra/archive/refs/tags/v3.1.tar.gz"
  sha256 "379894f36ef04a6b4e57e77112070e23dcc75569d1df98a8f128fe24a8b5e0b1"
  license "GPL-3.0-only"
  head "https://github.com/brosck/mantra.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "842c1638874415b352a089b14648c546cbe5b4d22dd1947dbc29e1ec2fcb5eeb"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    output = pipe_output(bin/"mantra", "https://brew.sh")
    assert_match "\"indexName\":\"brew_all\"", output
  end
end
