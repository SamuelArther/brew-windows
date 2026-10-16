class Proxify < Formula
  desc "Portable proxy for capturing, manipulating, and replaying HTTP/HTTPS traffic"
  homepage "https://projectdiscovery.io"
  url "https://github.com/projectdiscovery/proxify/archive/refs/tags/v0.0.16.tar.gz"
  sha256 "a156d8094ac5a31bc92029c1a9e02300caa5e2f189dc13949410d792e9f4e63c"
  license "MIT"
  head "https://github.com/projectdiscovery/proxify.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c137a0f8c66f5959006f4673802c3cf12af8025dc3b4cf733acbd7256751b8d6"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/proxify"
  end

  test do
    # Other commands start proxify, which causes Homebrew CI to time out
    assert_match version.to_s, shell_output("#{bin}/proxify -version 2>&1")
    assert_match "given config file 'brew' does not exist", shell_output("#{bin}/proxify -config brew 2>&1", 1)
  end
end
