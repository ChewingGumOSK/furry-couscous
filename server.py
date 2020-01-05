from flask import Flask,send_file,request
from google.cloud import texttospeech

app = Flask(__name__)

@app.route("/")
def hello():
	# Instantiates a client
	client = texttospeech.TextToSpeechClient()

	# Set the text input to be synthesized
	synthesis_input = texttospeech.types.SynthesisInput(text=request.args.get("text"))

	# Build the voice request, select the language code ("en-US") and the ssml
	# voice gender ("neutral
	voice = texttospeech.types.VoiceSelectionParams(
		language_code='fr-FR',
		name="fr-FR-Wavenet-B",
		ssml_gender=texttospeech.enums.SsmlVoiceGender.MALE)

	# Select the type of audio file you want returned
	audio_config = texttospeech.types.AudioConfig(
		audio_encoding=texttospeech.enums.AudioEncoding.LINEAR16)

	# Perform the text-to-speech request on the text input with the selected
	# voice parameters and audio file type
	response = client.synthesize_speech(synthesis_input, voice, audio_config)

	# The response's audio_content is binary.
	with open('output.wav', 'wb') as out:
		# Write the response to the output file.
		out.write(response.audio_content)
		print('Audio content written to file "output.wav"')
	return send_file("output.wav")

if __name__ == "__main__":
	app.run()
