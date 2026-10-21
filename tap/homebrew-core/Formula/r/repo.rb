class Repo < Formula
  include Language::Python::Shebang

  desc "Repository tool for Android development"
  homepage "https://source.android.com/source/developing.html"
  url "https://gerrit.googlesource.com/git-repo.git",
      tag:      "v2.70",
      revision: "46e9866ca0f161604bd45a0c3b7c7887c9415dcb"
  license "Apache-2.0"
  version_scheme 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "8d3c1eccc7cb43f5318c1011c1def4ef8002fd7c0149ab49dbbedb40e3321446"
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
