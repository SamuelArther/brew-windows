class Ipget < Formula
  desc "Retrieve files over IPFS and save them locally"
  homepage "https://github.com/ipfs/ipget/"
  url "https://github.com/ipfs/ipget/archive/refs/tags/v0.13.2.tar.gz"
  sha256 "e30a9f84633562b4c4d262b4c027260d43a6dcb47630215c7221dcc8066e8816"
  license "MIT"
  head "https://github.com/ipfs/ipget.git", branch: "master"

  deprecate! date: "2026-08-21", because: :repo_archived, replacement_formula: "kubo"
  disable! date: "2027-08-21", because: :repo_archived, replacement_formula: "kubo"

  # TODO: unpin go@1.26 when ipget supports go 1.27
  depends_on "go@1.26" => :build

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f35b87a1f0cea50e4635d2f0e5bafb440142e0df5921d83706e2ac47cd8e4c28"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    # Make sure correct version is reported
    assert_match version.to_s, shell_output("#{bin}/ipget --version")

    # An example content identifier (CID) used in IPFS docs:
    # https://docs.ipfs.tech/concepts/content-addressing/
    cid = "bafybeihkoviema7g3gxyt6la7vd5ho32ictqbilu3wnlo3rs7ewhnp7lly"
    system bin/"ipget", "ipfs://#{cid}/"
    assert_match "JPEG image data", shell_output("file #{cid}")
  end
end
