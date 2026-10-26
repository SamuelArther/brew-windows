class Asnmap < Formula
  desc "Quickly map organization network ranges using ASN information"
  homepage "https://github.com/projectdiscovery/asnmap"
  url "https://github.com/projectdiscovery/asnmap/archive/refs/tags/v1.2.0.tar.gz"
  sha256 "f4c615392cf206930361588a03b3df8f46775faabf37b84a398068dbe7159008"
  license "MIT"
  head "https://github.com/projectdiscovery/asnmap.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0afb1dea279374bf90ea3c782c3eed4aac9d2b667fbf62d44b7d5ba1b3bb165a"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/asnmap"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/asnmap -version 2>&1")

    # Skip linux CI test as test not working there
    return if !OS.mac? && ENV["HOMEBREW_GITHUB_ACTIONS"]

    # need API key for IP lookup test, thus just run empty binary test
    assert_match "no input defined", shell_output("#{bin}/asnmap 2>&1", 1)
  end
end
