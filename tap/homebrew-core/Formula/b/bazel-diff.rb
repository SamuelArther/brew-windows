class BazelDiff < Formula
  desc "Performs Bazel Target Diffing between two revisions in Git"
  homepage "https://github.com/Tinder/bazel-diff/"
  url "https://github.com/Tinder/bazel-diff/releases/download/v60.0.0/bazel-diff-rust-windows-amd64.exe"
  sha256 "c60161a7cee0e6495cf79f447a43bbde3ea8b4d2728cb573ea7dc8c30490995c"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Tinder/bazel-diff/archive/refs/tags/v60.0.0.tar.gz"
  end

  def install
    bin.install "bazel-diff-rust-windows-amd64.exe" => "bazel-diff.exe"
  end

  test do
    (testpath/"from.json").write <<~JSON
      {"//app:leaf": "Rule#old~old", "//app:top": "Rule#top~same"}
    JSON
    (testpath/"to.json").write <<~JSON
      {"//app:leaf": "Rule#new~new", "//app:top": "Rule#top~same"}
    JSON

    output = shell_output("#{bin}/bazel-diff get-impacted-targets --startingHashes from.json " \
                          "--finalHashes to.json --workspacePath #{testpath}")
    assert_equal "//app:leaf\n", output
  end
end
