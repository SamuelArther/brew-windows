class Goat < Formula
  desc "General purpose AT Protocol CLI in Go"
  homepage "https://github.com/bluesky-social/goat"
  url "https://github.com/bluesky-social/goat/releases/download/v0.2.6/goat_Windows_x86_64.zip"
  sha256 "66803250cdc8e324b34640c5e4c10d3b0e90736e6ef097da76ed935093c844be"
  license any_of: ["MIT", "Apache-2.0"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bluesky-social/goat/archive/refs/tags/v0.2.6.tar.gz"
  end

  def install
    bin.install "goat.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/goat --version 2>&1")
  end
end
