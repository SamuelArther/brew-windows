class Yor < Formula
  desc "Extensible auto-tagger for your IaC files"
  homepage "https://yor.io/"
  url "https://github.com/bridgecrewio/yor/releases/download/0.1.200/yor_0.1.200_windows_amd64.zip"
  sha256 "b3085bde997365f69444aa94f8e58b1b93edc64c7122758e5507efb86735aa4b"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bridgecrewio/yor/archive/refs/tags/0.1.200.tar.gz"
  end

  def install
    bin.install "yor.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/yor --version")

    assert_match "yor_trace", shell_output("#{bin}/yor list-tags")
    assert_match "code2cloud", shell_output("#{bin}/yor list-tag-groups")
  end
end
