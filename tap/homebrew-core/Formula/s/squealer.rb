class Squealer < Formula
  desc "Scans Git repositories or filesystems for secrets in commit histories"
  homepage "https://github.com/owenrumney/squealer"
  url "https://github.com/owenrumney/squealer/archive/refs/tags/v1.2.12.tar.gz"
  sha256 "c1a431addf696b7fb67d3c144c43293f3c4a7eb40096f7581e55e6525d76b2ea"
  license "Apache-2.0"
  head "https://github.com/owenrumney/squealer.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f12eaeebe0faf9422787fa947d6db8e5b25f61b60cfd20d5f85d8a276b99537d"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/owenrumney/squealer/version.Version=#{version}]
    system "go", "build", *std_go_args(ldflags:), "./cmd/squealer"
    generate_completions_from_executable(bin/"squealer", shell_parameter_format: :cobra)
  end

  test do
    system "git", "clone", "https://github.com/owenrumney/squealer.git"
    output = shell_output("#{bin}/squealer .", 1)
    assert_match "-----BEGIN OPENSSH PRIVATE KEY-----", output
  end
end
