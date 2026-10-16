class Cariddi < Formula
  desc "Scan for endpoints, secrets, API keys, file extensions, tokens and more"
  homepage "https://edoardottt.com/"
  url "https://github.com/edoardottt/cariddi/archive/refs/tags/v1.4.6.tar.gz"
  sha256 "9a33ebf9324c3f7f28c72161c80c994eaa3ca495c487eefed39a2e6861d65674"
  license "GPL-3.0-or-later"
  head "https://github.com/edoardottt/cariddi.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b2a830b3579aae121204ed1bbb564bbad44aac400acbda0ba2e3333d6d278f96"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/cariddi"
  end

  test do
    output = pipe_output(bin/"cariddi", "https://brew.sh/")
    assert_match %r{(https://brew.sh/*)}i, output

    assert_match version.to_s, shell_output("#{bin}/cariddi -version 2>&1")
  end
end
