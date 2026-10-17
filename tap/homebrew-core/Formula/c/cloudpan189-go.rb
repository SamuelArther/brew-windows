class Cloudpan189Go < Formula
  desc "Command-line client tool for Cloud189 web disk"
  homepage "https://github.com/tickstep/cloudpan189-go"
  url "https://github.com/tickstep/cloudpan189-go/archive/refs/tags/v0.1.3.tar.gz"
  sha256 "a215b75369af535aed214c94b66ebb3239b6ef5fcbc2f74039cf9c3eda4b04c1"
  license "Apache-2.0"
  head "https://github.com/tickstep/cloudpan189-go.git", branch: "master"

  deprecate! date: "2026-03-22", because: :repo_archived
  disable! date: "2027-03-22", because: :repo_archived

  bottle do
    sha256 cellar: :any, x86_64_windows: "20ad13e245e9d35d3622142a4d7387b1feeba3b31a5be16a91c4219c9d8a11a7"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    # TODO: remove `-checklinkname=0` workaround when fixed
    # https://github.com/tickstep/cloudpan189-go/issues/101
    system "go", "build", *std_go_args(ldflags: "-checklinkname=0")
  end

  test do
    system bin/"cloudpan189-go", "run", "touch", "output.txt"
    assert_path_exists testpath/"output.txt"
  end
end
