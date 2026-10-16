class Terrahash < Formula
  desc "Create and store a hash of the Terraform modules used by your configuration"
  homepage "https://github.com/ned1313/terrahash"
  url "https://github.com/ned1313/terrahash/releases/download/v0.1.0/terrahash_Windows_x86_64.zip"
  sha256 "8f539be3085a1fcae857a963bd6c6670de28cf12d96fa61cac6819c086398d27"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ned1313/terrahash/archive/refs/tags/v0.1.0.tar.gz"
  end

  def install
    bin.install "terrahash.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/terrahash version 2>&1")
  end
end
