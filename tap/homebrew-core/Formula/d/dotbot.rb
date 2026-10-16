class Dotbot < Formula
  desc "Tool that bootstraps your dotfiles"
  homepage "https://github.com/anishathalye/dotbot"
  url "https://github.com/anishathalye/dotbot/releases/download/v1.24.0/dotbot-windows-x64.zip"
  sha256 "c82e5f57612884bd0cfaa516f072687a4a48c0fbac9b633547d7045d649289cd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://files.pythonhosted.org/packages/ce/99/905f34404698d54de29fbc1dcbb9fdc4b1bbd4b5b30207750ff5ad5b5c69/dotbot-1.24.1.tar.gz"
  end

  def install
    bin.install "dotbot.exe"
  end

  test do
    (testpath/"install.conf.yaml").write <<~YAML
      - create:
        - brew
        - .brew/test
    YAML

    output = shell_output("#{bin}/dotbot --verbose -c #{testpath}/install.conf.yaml")
    assert_match "All tasks executed successfully", output
    assert_path_exists testpath/"brew"
    assert_path_exists testpath/".brew/test"
  end
end
