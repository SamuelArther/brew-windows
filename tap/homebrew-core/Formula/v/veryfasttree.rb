class Veryfasttree < Formula
  desc "Efficient phylogenetic tree inference for massive taxonomic datasets"
  homepage "https://github.com/citiususc/veryfasttree"
  url "https://github.com/citiususc/veryfasttree/releases/download/v4.0.5/VeryFastTree4.0.5Win64-OpenMP-AVX2.zip"
  sha256 "45040828d9bc75cf69e4325e6b3c884c40a6fcd0be32439adc9986634584e8b4"
  license all_of: [
    "GPL-3.0-only",
    "BSD-3-Clause", # libs/cli11
    "MPL-2.0", # libs/bxzstr
  ]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/citiususc/veryfasttree/archive/refs/tags/v4.0.5.tar.gz"
  end

  def install
    bin.install "VeryFastTree.exe"
  end

  test do
    assert_path_exists bin/"VeryFastTree.exe"
  end
end
