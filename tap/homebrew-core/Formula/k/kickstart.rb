class Kickstart < Formula
  desc "Scaffolding tool to get new projects up and running quickly"
  homepage "https://github.com/Keats/kickstart"
  url "https://github.com/Keats/kickstart/releases/download/v0.6.0/kickstart-v0.6.0-x86_64-pc-windows-msvc.zip"
  sha256 "cdb6b256695671a90a55fff2127556081174d256d163011ff28fe564e0bca7da"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Keats/kickstart/archive/refs/tags/v0.6.0.tar.gz"
  end

  def install
    bin.install "kickstart.exe"
  end

  test do
    # Create a basic template file and project, and check that kickstart
    # actually interpolates both the filename and its content.
    template_dir = testpath/"template"
    output_dir = testpath/"output"

    (template_dir/"{{file_name}}.txt").write("{{software_project}} is awesome!")

    (template_dir/"template.toml").write <<~TOML
      name = "Super basic"
      description = "A very simple template"
      kickstart_version = 1

      [[variables]]
      name = "file_name"
      default = "myfilename"
      prompt = "File name?"

      [[variables]]
      name = "software_project"
      default = "kickstart"
      prompt = "Which software project is awesome?"
    TOML

    # Run template interpolation
    system bin/"kickstart", "--no-input", "--output-dir", output_dir, template_dir

    assert_path_exists output_dir/"myfilename.txt"
    assert_equal "kickstart is awesome!", (output_dir/"myfilename.txt").read
  end
end
