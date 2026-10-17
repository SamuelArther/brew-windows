class GitPkgs < Formula
  desc "Track package dependencies across git history"
  homepage "https://git-pkgs.dev"
  url "https://github.com/git-pkgs/git-pkgs/archive/refs/tags/v0.21.0.tar.gz"
  sha256 "d48505757969fb808e21de80f3dc8d8247e3cfd3f8f89c30bf43fe7a91d5f598"
  license "MIT"
  head "https://github.com/git-pkgs/git-pkgs.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a722bf6bb1f38ae208cfa2bc8d2a52686a856849c943018b5e6c3465f07f7289"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/git-pkgs/git-pkgs/cmd.version=#{version}
      -X github.com/git-pkgs/git-pkgs/cmd.commit=HEAD
      -X github.com/git-pkgs/git-pkgs/cmd.date=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:)

    system "go", "run", "scripts/generate-man/main.go"
    man1.install Dir["man/*.1"]

    generate_completions_from_executable(bin/"git-pkgs", "completion")
  end

  test do
    system "git", "init"
    File.write("package.json", '{"dependencies":{"lodash":"^4.17.21"}}')
    system bin/"git-pkgs", "diff-file", "package.json", "package.json"
    assert_match version.to_s, shell_output("#{bin/"git-pkgs"} --version")
  end
end
