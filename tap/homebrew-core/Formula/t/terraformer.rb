class Terraformer < Formula
  desc "CLI tool to generate terraform files from existing infrastructure"
  homepage "https://github.com/GoogleCloudPlatform/terraformer"
  url "https://github.com/GoogleCloudPlatform/terraformer/archive/refs/tags/0.8.30.tar.gz"
  sha256 "9e4738fadae011e458fa6fee168f47166cd3a9f5d7a9378018116345b0d6b4e6"
  license "Apache-2.0"
  head "https://github.com/GoogleCloudPlatform/terraformer.git", branch: "master"

  # https://github.com/GoogleCloudPlatform/terraformer/commit/1807affdde9e392b8b8749932d8cd1daa5e4986e
  deprecate! date: "2026-06-08", because: :repo_archived
  disable! date: "2027-06-08", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "38de0633b83b32d5e73726690138f9a96185fd2a63bd5503585e604d897d2656"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    # Work around failure: ld: B/BL out of range -162045188 (max +/-128MB)
    ldflags = "-extldflags=-ld_classic" if DevelopmentTools.ld64_version.between?("1015.7", "1022.1")

    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    assert_match version.to_s,
      shell_output("#{bin}/terraformer version")

    assert_match "Available Commands",
      shell_output("#{bin}/terraformer -h")

    assert_match "aaa",
      shell_output("#{bin}/terraformer import google --resources=gcs --projects=aaa 2>&1", 1)
  end
end
