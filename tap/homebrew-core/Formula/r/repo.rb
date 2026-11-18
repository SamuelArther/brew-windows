class Repo < Formula
  include Language::Python::Shebang

  desc "Repository tool for Android development"
  homepage "https://source.android.com/source/developing.html"
  url "https://gerrit.googlesource.com/git-repo.git",
      tag:      "v2.74",
      revision: "a816b88d2319089d5cbaa3fa336a68db917cb0f7"
  license "Apache-2.0"
  version_scheme 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "9686c8094db0af92e5a194f506857e65a277f5e4744b44ad494a989d2ec070c1"
  end

  uses_from_macos "python"

  deny_network_access!

  def install
    bin.install "repo"
    doc.install (buildpath/"docs").children
    bash_completion.install "completion.bash" => "repo" if OS.linux? # needs GNU sed
    zsh_completion.install "completion.zsh" => "_repo"
    man1.install Utils::Gzip.compress(*Dir["man/*.1"])

    rewrite_shebang detected_python_shebang(use_python_from_path: true), bin/"repo"
  end

  test do
    assert_match "usage:", shell_output("#{bin}/repo help 2>&1")
  end
end
