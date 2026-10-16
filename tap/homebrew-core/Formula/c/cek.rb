class Cek < Formula
  desc "Explore the (overlay) filesystem and layers of OCI container images"
  homepage "https://github.com/bschaatsbergen/cek"
  url "https://github.com/bschaatsbergen/cek/archive/refs/tags/v0.3.0.tar.gz"
  sha256 "df2e264e15b7e5d2d72146090300ad6833801213e552a55c9079449d8b8a71d8"
  license "MIT"
  head "https://github.com/bschaatsbergen/cek.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8f7d33b91aeb2340a313ec8eb0e6bef71a5a73be2020a7c5fbed58dfb5f4bc2e"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/bschaatsbergen/cek/version.Version=#{version}]
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    assert_includes shell_output("#{bin}/cek version"), "cek version #{version}"
    assert_match "localhost", shell_output("#{bin}/cek cat alpine:latest /etc/hostname")
  end
end
