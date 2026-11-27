class Proxify < Formula
  desc "Portable proxy for capturing, manipulating, and replaying HTTP/HTTPS traffic"
  homepage "https://projectdiscovery.io"
  license "MIT"
  head "https://github.com/projectdiscovery/proxify.git", branch: "main"

  stable do
    url "https://github.com/projectdiscovery/proxify/archive/refs/tags/v0.0.17.tar.gz"
    sha256 "e0c5bc3c8b0666ccbfd34cadbab972ddfe3737f01f1bcde9fddb265c96262a76"

    # Report the version of the release being packaged.
    patch do
      url "https://github.com/projectdiscovery/proxify/commit/af4d51b66733f93dae11c775c2ede39763bc4fe6.patch?full_index=1"
      sha256 "3135398ce9478934784965c62f2ba66f235343ecad30d50399d8841a303ea82c"
      type :unofficial
      resolves "https://github.com/projectdiscovery/proxify/pull/791"
    end
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9334238c184b54c0ef03807395d9afaf950757040823f006f24a9f7f1e34acaa"
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
