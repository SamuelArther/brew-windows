class ConfigFileValidator < Formula
  desc "CLI tool to validate different configuration file types"
  homepage "https://boeing.github.io/config-file-validator/"
  url "https://github.com/Boeing/config-file-validator/archive/refs/tags/v3.0.0.tar.gz"
  sha256 "987931434d0fe2b5bdc4fc4c630a7c2f2f05ca51d1245f5c269c139a9e9e296c"
  license "Apache-2.0"
  head "https://github.com/Boeing/config-file-validator.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f65dfbbf5953828ede306ba447940a5d788fcb643ec8188696045817e649cee4"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/Boeing/config-file-validator/v3.version=#{version}"
    system "go", "build", *std_go_args(ldflags:, output: bin/"cfv"), "./cmd/cfv"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cfv -version")

    test_file = testpath/"test.json"
    test_file.write <<~JSON
      { "valid": "json" }
    JSON
    # Windows prints the path with backslashes.
    assert_match "✓ #{OS.windows? ? test_file.to_s.tr("/", "\\") : test_file}", shell_output("#{bin}/cfv #{test_file}")
  end
end
