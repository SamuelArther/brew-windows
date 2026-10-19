class Smimesign < Formula
  desc "S/MIME signing utility for use with Git"
  homepage "https://github.com/github/smimesign"
  url "https://github.com/github/smimesign/archive/refs/tags/v0.2.0.tar.gz"
  sha256 "b5921dc3f3b446743e130d1ee39ab9ed2e256b001bd52cf410d30a0eb087f54e"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c43abc20a300886382bd757110171c3dff0c769dd52edd69a47ebf4f62e09678"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X main.versionString=#{version}"
    # Newer cgo won't convert a *C.char straight to HLOCAL (a pointer type) for LocalFree.
    inreplace "certstore/certstore_windows.go", "C.HLOCAL(cmsg)", "C.HLOCAL(unsafe.Pointer(cmsg))" if OS.windows?
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/smimesign --version")
    system bin/"smimesign", "--list-keys"
    assert_match "could not find identity matching specified user-id: bad@identity",
      shell_output("#{bin}/smimesign -su bad@identity 2>&1", 1)
  end
end
