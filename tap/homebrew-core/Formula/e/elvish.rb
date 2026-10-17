class Elvish < Formula
  desc "Friendly and expressive shell"
  homepage "https://elv.sh/"
  url "https://github.com/elves/elvish/archive/refs/tags/v0.21.0.tar.gz"
  sha256 "3a4b93c3c99fe2f9847de35d64be24e2d4b9c12d429cd9831b4571993a66bb7a"
  license "BSD-2-Clause"
  head "https://github.com/elves/elvish.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8ff596955512d5c043305ae2203272c2e75a94d6fffe4b60719324a9dd1b1902"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X src.elv.sh/pkg/buildinfo.VersionSuffix="
    system "go", "build", *std_go_args(ldflags:), "./cmd/elvish"
  end

  test do
    assert_equal version.to_s, shell_output("#{bin}/elvish -version").chomp
    assert_match "hello", shell_output("#{bin}/elvish -c 'echo hello'")
  end
end
