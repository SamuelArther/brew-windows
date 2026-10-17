class DockerLs < Formula
  desc "Tools for browsing and manipulating docker registries"
  homepage "https://github.com/mayflower/docker-ls"
  url "https://github.com/mayflower/docker-ls.git",
      tag:      "v0.5.1",
      revision: "ae0856513066feff2ee6269efa5d665145709d2e"
  license "MIT"
  head "https://github.com/mayflower/docker-ls.git", branch: "master"

  deprecate! date: "2026-02-17", because: :repo_archived
  disable! date: "2027-02-17", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2ae522a5d669210a0ca789681387c50519dd57f1c91e9425a0be5d3835e12c09"
  end

  depends_on "go" => :build

  def install
    system "go", "generate", "./lib"

    %w[docker-ls docker-rm].each do |name|
      system "go", "build", *std_go_args(output: bin/name), "./cli/#{name}"
    end
  end

  test do
    assert_match(/\Wlatest\W/m, pipe_output("#{bin}/docker-ls tags \
      -r https://index.docker.io -u '' -p '' \
      --progress-indicator=false library/busybox
    "))

    assert_match "401", pipe_output("#{bin}/docker-rm  \
      -r https://index.docker.io -u foo -p bar library/busybox:latest 2<&1
    ")
  end
end
